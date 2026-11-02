// Recorrido real de pantallas con Chromium headless (k6 browser) contra el frontend local (http://localhost:5081)
// conectado a la API local (http://127.0.0.1:18080). Adecuación funcional (CU-01, CU-02, RF-020/021/023) y
// eficiencia de desempeño del cliente (arranque en frío de Blazor con red móvil simulada, Web Vitals).
// Ejecutar con: docker run --rm --network host -v <pruebas>:/guiones:ro -v <salida>:/salida \
//   -e WEB=http://localhost:5081 -e CLAVE=... -e USUARIO=... grafana/k6:0.54.0-with-browser run /guiones/funcional/recorrido-navegador.js
import { browser } from 'k6/browser';
import { check } from 'k6';
import { Trend } from 'k6/metrics';

const WEB = __ENV.WEB || 'http://localhost:5081';
const CLAVE = __ENV.CLAVE; const USUARIO = __ENV.USUARIO; const ADMIN = __ENV.ADMIN || 'qa.admin@itm.test';
// Ronda 2: pantallas F2–F7. PUNTO = id de un punto de acceso; SESION = id de una sesión de ENTRENADOR; ENTRENADOR = correo de su dueño.
const PUNTO = __ENV.PUNTO; const SESION = __ENV.SESION; const ENTRENADOR = __ENV.ENTRENADOR || 'qa.entrenador1@itm.test';
const arranqueFrio = new Trend('cliente_arranque_frio_ms', true);
const arranqueRed = new Trend('cliente_arranque_4g_lento_ms', true);
const cargaEventos = new Trend('cliente_lista_eventos_ms', true);

export const options = {
  scenarios: { ui: { executor: 'shared-iterations', iterations: 1, vus: 1, options: { browser: { type: 'chromium' } } } },
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)'],
};

// Captura solo la ventana visible: la lista de eventos sin paginar mide >600 000 px de alto (hallazgo de desempeño)
async function captura(p, n) { await p.screenshot({ path: `/salida/ui-${n}.png` }); }
async function iniciar(p, correo) {
  await p.goto(`${WEB}/login`);
  await p.locator('#emailInput').waitFor({ timeout: 60000 });
  await p.locator('#emailInput').fill(correo); await p.locator('#passwordInput').fill(CLAVE);
  await p.locator('button[type="submit"]').click();
  await p.waitForFunction(() => !location.pathname.startsWith('/login'), { timeout: 20000 });
}

export default async function () {
  const errores = [];
  // 1) Arranque en frío, perfil 4G lento (latencia 150 ms, 5 Mbps ≈ 625 000 B/s), viewport de celular.
  const ctxRed = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const pr = await ctxRed.newPage();
  await pr.throttleNetwork({ latency: 150, download: 625000, upload: 250000 });
  let t = Date.now(); await pr.goto(`${WEB}/login`); await pr.locator('#emailInput').waitFor({ timeout: 120000 });
  arranqueRed.add(Date.now() - t); await captura(pr, '01-login-4g-lento'); await pr.close(); await ctxRed.close();

  // 2) Usuario en celular: login → eventos → reservar → mis reservas → mi historial
  const ctx = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const p = await ctx.newPage();
  p.on('console', (m) => { if (m.type() === 'error') errores.push(m.text()); });
  try {
    t = Date.now(); await p.goto(`${WEB}/login`); await p.locator('#emailInput').waitFor({ timeout: 60000 }); arranqueFrio.add(Date.now() - t);
    await iniciar(p, USUARIO);
    check(p, { 'CU-01: inicio de sesión lleva fuera de /login': () => !p.url().includes('/login') });
    t = Date.now(); await p.goto(`${WEB}/eventos`);
    const reservar = p.locator('button.btn-itm-primario >> nth=0');
    await reservar.waitFor({ timeout: 30000 }); cargaEventos.add(Date.now() - t);
    await captura(p, '02-eventos');
    const dom = JSON.parse(await p.evaluate(() => JSON.stringify([document.querySelectorAll('button.btn-itm-primario').length, document.body.scrollHeight, document.getElementsByTagName('*').length])));
    dom.tarjetas = dom[0]; dom.alto = dom[1]; dom.nodos = dom[2];
    console.log(`EVENTOS_DOM tarjetas=${dom.tarjetas} alto_px=${dom.alto} nodos=${dom.nodos}`);
    check(dom.tarjetas, { 'RF-001: la lista de eventos muestra sesiones reservables': (n) => n > 0 });
    // Hallazgo: el primer botón "Reservar cupo" de la vista "Todos" puede ser una sesión YA PASADA.
    await reservar.click();
    await p.locator('[role="alert"]').waitFor({ timeout: 15000 });
    const primera = await p.locator('[role="alert"]').textContent();
    console.log(`PRIMER_BOTON_RESERVAR: ${primera}`);
    check(primera, { 'RF-001/RN-009: el primer "Reservar cupo" de la lista es reservable (no es una sesión pasada)': (x) => !/comenzado|consolidad/i.test(x || '') });
    // Reserva en el día de mañana (chip de fecha), que sí es reservable
    const chip = await p.evaluate((f) => { const b = [...document.querySelectorAll('button')].find((x) => x.innerText.includes(f) || x.innerText.includes('Mañana')); if (b) { b.click(); return b.innerText; } return ''; }, __ENV.MANANA);
    console.log(`CHIP_DIA: ${chip}`);
    await p.waitForTimeout(800);
    await p.evaluate(() => { const b = [...document.querySelectorAll('button.btn-itm-primario')].find((x) => /Reservar/.test(x.innerText)); if (b) b.click(); });
    await p.waitForTimeout(2500);
    const exito = await p.evaluate(() => document.querySelector('[role="alert"]') ? document.querySelector('[role="alert"]').className + ' :: ' + document.querySelector('[role="alert"]').innerText : '');
    console.log(`RESERVA_MANANA: ${exito}`);
    // El primer clic de arriba ya reservó la primera sesión reservable; si es la misma de 'mañana', el aviso correcto es 'Ya tienes una reserva'.
    check(exito, { 'CU-01: reservar una sesión de mañana muestra éxito (o aviso de reserva ya existente)': (x) => /alert-success|Ya tienes una reserva/.test(x) });
    await captura(p, '03-reserva-hecha');
    await p.goto(`${WEB}/reservas`); await p.waitForTimeout(3000); await captura(p, '04-mis-reservas');
    const txtRes = await p.evaluate(() => document.body.innerText);
    check(txtRes, { 'RF-020: Mis reservas lista la reserva': (x) => !/No tienes reservas/i.test(x) && /Confirmada|espera/i.test(x) });
    check(txtRes, { 'RN-020: sin nivel/indicador de priorización en Mis reservas': (x) => !/prioriz|indicador|penalizad|nivel/i.test(x) });
    const cancelar = p.locator('button.btn-outline-danger >> nth=0');
    if (await cancelar.isVisible()) {
      await cancelar.click(); await p.waitForTimeout(1500); await captura(p, '05-confirmar-cancelacion');
    }
    await p.goto(`${WEB}/mi-historial`); await p.waitForTimeout(3000); await captura(p, '06-mi-historial');
    check(await p.evaluate(() => document.body.innerText), { 'RF-021: Mi historial carga sin error': (x) => !/error inesperado|no se pudo/i.test(x) });
  } finally { await p.close(); await ctx.close(); }

  // 3) Administrador en escritorio: usuarios (API real) y reportes (servicio en memoria → detectar datos simulados)
  const ctxA = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const a = await ctxA.newPage();
  try {
    await iniciar(a, ADMIN);
    await a.goto(`${WEB}/admin/usuarios`); await a.waitForTimeout(4000); await captura(a, '07-admin-usuarios');
    check(await a.evaluate(() => document.body.innerText), { 'RF-023: gestión de usuarios muestra cuentas reales (qa.)': (x) => /qa\.(usuario|entrenador|admin)/i.test(x) });
    await a.goto(`${WEB}/admin/reportes`); await a.waitForTimeout(4000); await captura(a, '08-admin-reportes');
    const rep = await a.evaluate(() => document.body.innerText);
    check(rep, { 'RF-033: reportes reflejan datos reales (eventos "QA ...")': (x) => /QA (Volumen|Pico|Check-in)/.test(x) });
    await a.goto(`${WEB}/admin/auditoria`); await a.waitForTimeout(4000); await captura(a, '09-admin-auditoria');
    check(await a.evaluate(() => document.body.innerText), { 'RF-032: auditoría muestra acciones reales (Eventos.Crear)': (x) => /Eventos\.Crear|QA Volumen|Usuarios\./.test(x) });
  } finally { await a.close(); await ctxA.close(); }
  // 4) Ronda 2 · pantallas F2–F7: ¿muestran datos reales del backend o datos simulados? (H-01, H-02, H-19)
  const ctxB = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const b = await ctxB.newPage();
  b.on('console', (m) => { if (m.type() === 'error') errores.push(m.text()); });
  const texto = () => b.evaluate(() => document.body.innerText);
  try {
    await iniciar(b, ADMIN);
    await b.goto(`${WEB}/admin/reportes`); await b.waitForTimeout(5000); await captura(b, '10-r2-reportes');
    const rep2 = await texto();
    console.log(`REPORTES_FICTICIOS=${/Spinning Matutino|Crossfit Intenso|Natación Libre/.test(rep2)}`);
    check(rep2, { 'H-01 · Reportes: sin actividades ficticias (Spinning/Crossfit/Natación)': (x) => !/Spinning Matutino|Crossfit Intenso|Natación Libre/.test(x) });
    await b.goto(`${WEB}/admin/auditoria`); await b.waitForTimeout(5000); await captura(b, '11-r2-auditoria');
    const aud = await texto();
    console.log(`AUDITORIA_FICTICIA=${/Admin Sistema|pedro\.entrenador/.test(aud)}`);
    check(aud, { 'H-01 · Auditoría: sin actores ficticios (Admin Sistema / pedro.entrenador)': (x) => !/Admin Sistema|pedro\.entrenador/.test(x) });
    const pag = await b.evaluate(() => [...document.querySelectorAll('button')].filter((x) => /Anterior|Siguiente/.test(x.innerText)).map((x) => /<svg|bi-|icon/i.test(x.innerHTML) ? 'icono' : 'sin-icono'));
    console.log(`PAGINACION_AUDITORIA=${pag.join(',')}`);
    await b.goto(`${WEB}/admin/calendario`); await b.waitForTimeout(4000); await captura(b, '12-r2-calendario');
    check(await texto(), { 'H-01 · Calendario de bloqueos muestra los bloqueos reales del backend ("Festivo probe" / "Mantenimiento QA2")': (x) => /Festivo probe|Mantenimiento QA2/.test(x) });
    await b.goto(`${WEB}/admin/retroalimentacion`); await b.waitForTimeout(4000); await captura(b, '13-r2-bandeja');
    check(await texto(), { 'H-01 · Bandeja de retroalimentación muestra reportes reales del backend (prueba de limite / No carga la lista)': (x) => /prueba de limite|No carga la lista|script/.test(x) });
    if (SESION) {
      await b.goto(`${WEB}/admin/sesiones/${SESION}`); await b.waitForTimeout(4000); await captura(b, '14-r2-admin-sesion');
      check(await texto(), { 'H-01 · Administrar sesión carga la sesión real (sin error)': (x) => !/no encontrada|error inesperado|no se pudo/i.test(x) });
    }
    if (PUNTO) {
      await b.goto(`${WEB}/admin/puntos-acceso/${PUNTO}/pantalla`); await b.waitForTimeout(4000); await captura(b, '15-r2-pantalla-punto');
      const qr = await b.evaluate(() => document.documentElement.innerHTML);
      const pideApi = qr.includes('dyn_');
      console.log(`QR_PANTALLA_TOKEN_SIMULADO_EN_DOM=${pideApi}`);
      check(qr, { 'H-02 · La pantalla del punto muestra el punto real del backend ("QA Torniquete"), no un nombre fijo': (x) => x.includes('QA Torniquete') });
    }
  } finally { await b.close(); await ctxB.close(); }
  if (SESION) {
    const ctxE = await browser.newContext({ viewport: { width: 390, height: 844 } });
    const e = await ctxE.newPage();
    try {
      await iniciar(e, ENTRENADOR);
      await e.goto(`${WEB}/entrenador/sesiones/${SESION}/notas`); await e.waitForTimeout(4000); await captura(e, '16-r2-notas');
      check(await e.evaluate(() => document.body.innerText), { 'H-01 · Notas de sesión muestran las notas reales (Nota privada QA2)': (x) => /Nota privada QA2/.test(x) });
      await e.goto(`${WEB}/entrenador/sesiones/${SESION}/asistencia`); await e.waitForTimeout(4000); await captura(e, '17-r2-asistencia-manual');
      check(await e.evaluate(() => document.body.innerText), { 'H-01 · Asistencia manual lista a inscritos reales (qa.usuario0030)': (x) => /QA Usuario 0030|qa\.usuario0030/.test(x) });
    } finally { await e.close(); await ctxE.close(); }
  }
  console.log(`ERRORES_CONSOLA=${errores.length} ${errores.slice(0, 5).join(' | ')}`);
}
