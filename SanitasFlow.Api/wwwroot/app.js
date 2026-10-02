"use strict";

/* =========================================================================
   SanitasFlow - interfaz web
   -------------------------------------------------------------------------
   La información persistente se obtiene de la API ASP.NET Core y PostgreSQL.
   sessionStorage solo conserva el borrador entre la selección
   del horario y la confirmación de la cita.
   ========================================================================= */

function $(selector, scope) {
    return (scope || document).querySelector(selector);
}

function $all(selector, scope) {
    return Array.from((scope || document).querySelectorAll(selector));
}

/* ---------------------------------------------------------------------
   Datos de especialidades y procedimientos
   duracionMin: minutos que ocupa el procedimiento en la agenda.
   precio: pesos mexicanos (MXN), aproximado.
--------------------------------------------------------------------- */
const SPECIALTIES = [
    {
        id: "cardiologia",
        nombre: "Cardiología",
        icono: "❤️",
        descripcion: "Diagnóstico y seguimiento de enfermedades del corazón.",
        procedimientos: [
            { id: "card-consulta", nombre: "Consulta cardiológica general", duracionMin: 40, precio: 800, descripcion: "Valoración inicial y revisión de antecedentes. Costo aprox. $800 MXN." },
            { id: "card-ekg", nombre: "Electrocardiograma", duracionMin: 20, precio: 500, descripcion: "Registro de la actividad eléctrica del corazón. Costo aprox. $500 MXN." },
            { id: "card-eco", nombre: "Ecocardiograma", duracionMin: 45, precio: 1500, descripcion: "Ultrasonido para evaluar estructura y función cardiaca. Costo aprox. $1,500 MXN." },
            { id: "card-esfuerzo", nombre: "Prueba de esfuerzo", duracionMin: 60, precio: 2200, descripcion: "Evaluación cardiaca bajo actividad física controlada. Costo aprox. $2,200 MXN." },
            { id: "card-holter", nombre: "Holter 24 horas (instalación)", duracionMin: 30, precio: 1800, descripcion: "Monitoreo continuo del ritmo cardiaco por 24 horas. Costo aprox. $1,800 MXN." }
        ]
    },
    {
        id: "dermatologia",
        nombre: "Dermatología",
        icono: "🧴",
        descripcion: "Diagnóstico y tratamiento de la piel, cabello y uñas.",
        procedimientos: [
            { id: "derm-consulta", nombre: "Consulta dermatológica", duracionMin: 30, precio: 700, descripcion: "Valoración general de piel, cabello o uñas. Costo aprox. $700 MXN." },
            { id: "derm-lunares", nombre: "Revisión de lunares", duracionMin: 30, precio: 650, descripcion: "Revisión y seguimiento de lunares o manchas. Costo aprox. $650 MXN." },
            { id: "derm-crio", nombre: "Crioterapia", duracionMin: 20, precio: 500, descripcion: "Eliminación de verrugas o lesiones con frío controlado. Costo aprox. $500 MXN." },
            { id: "derm-biopsia", nombre: "Biopsia de piel", duracionMin: 45, precio: 1400, descripcion: "Toma de muestra de piel para análisis. Costo aprox. $1,400 MXN." },
            { id: "derm-acne", nombre: "Tratamiento de acné", duracionMin: 30, precio: 600, descripcion: "Valoración y tratamiento dirigido de acné. Costo aprox. $600 MXN." }
        ]
    },
    {
        id: "pediatria",
        nombre: "Pediatría",
        icono: "🧸",
        descripcion: "Atención médica para bebés, niñas, niños y adolescentes.",
        procedimientos: [
            { id: "ped-consulta", nombre: "Consulta pediátrica", duracionMin: 30, precio: 600, descripcion: "Valoración general del paciente pediátrico. Costo aprox. $600 MXN." },
            { id: "ped-nino-sano", nombre: "Control de niño sano", duracionMin: 30, precio: 550, descripcion: "Seguimiento de crecimiento y desarrollo. Costo aprox. $550 MXN." },
            { id: "ped-vacuna", nombre: "Vacunación", duracionMin: 15, precio: 400, descripcion: "Aplicación de esquema de vacunación. Costo aprox. $400 MXN (precio varía según vacuna)." },
            { id: "ped-desarrollo", nombre: "Valoración de desarrollo", duracionMin: 40, precio: 700, descripcion: "Evaluación de hitos de desarrollo del menor. Costo aprox. $700 MXN." }
        ]
    },
    {
        id: "ginecologia",
        nombre: "Ginecología",
        icono: "🩺",
        descripcion: "Salud reproductiva y seguimiento ginecológico.",
        procedimientos: [
            { id: "gine-consulta", nombre: "Consulta ginecológica", duracionMin: 40, precio: 750, descripcion: "Valoración general ginecológica. Costo aprox. $750 MXN." },
            { id: "gine-papanicolaou", nombre: "Papanicolaou", duracionMin: 20, precio: 500, descripcion: "Estudio de detección oportuna de cáncer cervicouterino. Costo aprox. $500 MXN." },
            { id: "gine-usg", nombre: "Ultrasonido pélvico", duracionMin: 30, precio: 900, descripcion: "Estudio de imagen de órganos pélvicos. Costo aprox. $900 MXN." },
            { id: "gine-prenatal", nombre: "Control prenatal", duracionMin: 30, precio: 700, descripcion: "Seguimiento del embarazo. Costo aprox. $700 MXN." },
            { id: "gine-colposcopia", nombre: "Colposcopía", duracionMin: 40, precio: 1300, descripcion: "Estudio detallado del cuello uterino. Costo aprox. $1,300 MXN." }
        ]
    },
    {
        id: "traumatologia",
        nombre: "Traumatología",
        icono: "🦴",
        descripcion: "Lesiones, fracturas y rehabilitación del sistema músculo-esquelético.",
        procedimientos: [
            { id: "trauma-consulta", nombre: "Consulta traumatológica", duracionMin: 40, precio: 750, descripcion: "Valoración de lesiones óseas, musculares o articulares. Costo aprox. $750 MXN." },
            { id: "trauma-infiltracion", nombre: "Infiltración articular", duracionMin: 30, precio: 1100, descripcion: "Aplicación de medicamento directo en la articulación. Costo aprox. $1,100 MXN." },
            { id: "trauma-postop", nombre: "Revisión postoperatoria", duracionMin: 20, precio: 500, descripcion: "Seguimiento posterior a una cirugía. Costo aprox. $500 MXN." },
            { id: "trauma-yeso", nombre: "Inmovilización / yeso", duracionMin: 30, precio: 650, descripcion: "Colocación de férula o yeso. Costo aprox. $650 MXN." },
            { id: "trauma-rehab", nombre: "Rehabilitación (sesión)", duracionMin: 45, precio: 600, descripcion: "Sesión de terapia física. Costo aprox. $600 MXN." }
        ]
    }
];

const GENERAL_SPECIALTY = {
    id: "general",
    nombre: "Cita general",
    icono: "✚",
    descripcion: "Consulta de medicina general para cualquier malestar o duda de salud.",
    procedimientos: [
        { id: "gen-consulta", nombre: "Consulta general", duracionMin: 30, precio: 450, descripcion: "Valoración médica general. Costo aprox. $450 MXN." },
        { id: "gen-revision", nombre: "Revisión rápida", duracionMin: 20, precio: 350, descripcion: "Revisión breve de un malestar puntual. Costo aprox. $350 MXN." },
        { id: "gen-seguimiento", nombre: "Receta y seguimiento", duracionMin: 15, precio: 300, descripcion: "Renovación de receta o seguimiento de tratamiento. Costo aprox. $300 MXN." },
        { id: "gen-certificado", nombre: "Certificado médico", duracionMin: 20, precio: 350, descripcion: "Certificado médico general. Costo aprox. $350 MXN." }
    ]
};

function getAllSpecialties() {
    return [GENERAL_SPECIALTY, ...SPECIALTIES];
}

function findSpecialty(specialtyId) {
    return getAllSpecialties().find((s) => s.id === specialtyId) || null;
}

function findProcedure(specialtyId, procedureId) {
    const specialty = findSpecialty(specialtyId);
    if (!specialty) return null;
    const procedure = specialty.procedimientos.find((p) => p.id === procedureId);
    return procedure ? { specialty, procedure } : null;
}

/* ---------------------------------------------------------------------
   Horario real de atención
   Lunes a viernes: 09:00-14:00 y 15:00-18:00 (1 hora de comida)
   Sábado: 09:00-13:00
   Domingo: cerrado
--------------------------------------------------------------------- */
const WORKING_WINDOWS = {
    1: [[9 * 60, 14 * 60], [15 * 60, 18 * 60]],
    2: [[9 * 60, 14 * 60], [15 * 60, 18 * 60]],
    3: [[9 * 60, 14 * 60], [15 * 60, 18 * 60]],
    4: [[9 * 60, 14 * 60], [15 * 60, 18 * 60]],
    5: [[9 * 60, 14 * 60], [15 * 60, 18 * 60]],
    6: [[9 * 60, 13 * 60]],
    0: []
};

const SLOT_STEP_MIN = 15;

function pad2(n) {
    return String(n).padStart(2, "0");
}

function dateKey(date) {
    return `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`;
}

function minutesToLabel(mins) {
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    const suffix = h >= 12 ? "p.m." : "a.m.";
    const h12 = h % 12 === 0 ? 12 : h % 12;
    return `${h12}:${pad2(m)} ${suffix}`;
}

function nextAvailableDates(count) {
    const dates = [];
    const cursor = new Date();
    cursor.setHours(0, 0, 0, 0);
    while (dates.length < count) {
        const windows = WORKING_WINDOWS[cursor.getDay()];
        if (windows && windows.length) {
            dates.push(new Date(cursor));
        }
        cursor.setDate(cursor.getDate() + 1);
    }
    return dates;
}

/**
 * Solicita al servidor los horarios disponibles. La validación definitiva
 * de traslapes ocurre dentro de la transacción que crea la cita.
 */
async function generateSlots(date, procedureId, resourceId) {
    const query = new URLSearchParams({
        especialidadId: resourceId,
        procedimientoId: procedureId,
        fecha: dateKey(date)
    });
    const result = await api(`/citas/disponibilidad?${query}`);
    return result.horas;
}

/* ---------------------------------------------------------------------
   API y sesión
   La cookie de autenticación es HTTP-only; el navegador la envía, pero
   JavaScript no puede leerla ni fabricar una sesión.
--------------------------------------------------------------------- */
const API_BASE_URL = "/api";
let currentSession = null;

async function api(path, options = {}) {
    const headers = { Accept: "application/json", ...(options.headers || {}) };
    if (options.body) headers["Content-Type"] = "application/json";
    const response = await fetch(`${API_BASE_URL}${path}`, {
        ...options,
        headers,
        credentials: "include"
    });
    if (response.status === 204) return null;
    const data = await response.json().catch(() => null);
    if (!response.ok) throw new Error(data?.message || "No fue posible completar la solicitud.");
    return data;
}

async function refreshSession() {
    try {
        currentSession = await api("/auth/me");
    } catch (error) {
        currentSession = null;
    }
    return currentSession;
}

function getSession() {
    return currentSession;
}

function setSession(user) {
    currentSession = user;
}

async function clearSession() {
    try {
        await api("/auth/logout", { method: "POST" });
    } catch (error) {
        // La cookie puede haber expirado; aun así se limpia el estado visual.
    }
    currentSession = null;
}

async function registerUser(fields) {
    const user = await api("/auth/register", {
        method: "POST",
        body: JSON.stringify({ ...fields, edad: Number(fields.edad) })
    });
    setSession(user);
    return user;
}

async function loginUser(correo, password) {
    const user = await api("/auth/login", {
        method: "POST",
        body: JSON.stringify({ correo, password })
    });
    setSession(user);
    return user;
}

/* ---------------------------------------------------------------------
   Encabezado: mostrar nombre si hay sesión, o enlaces de acceso si no
--------------------------------------------------------------------- */
function renderHeaderAuth() {
    const slot = $("#auth-slot");
    if (!slot) return;
    const session = getSession();

    slot.innerHTML = "";

    if (session) {
        const name = document.createElement("a");
        name.className = "header-user";
        name.href = "bienvenida-paciente.html";
        name.textContent = session.nombres;
        name.title = "Ver mis datos y mis próximas citas";
        slot.append(name);

        const logout = document.createElement("button");
        logout.type = "button";
        logout.className = "link-ghost";
        logout.textContent = "Cerrar sesión";
        logout.addEventListener("click", async () => {
            await clearSession();
            window.location.reload();
        });
        slot.append(logout);
    } else {
        const login = document.createElement("a");
        login.href = "registro-paciente.html?tab=login";
        login.className = "link-ghost";
        login.textContent = "Iniciar sesión";

        const register = document.createElement("a");
        register.href = "registro-paciente.html?tab=registro";
        register.className = "link-ghost";
        register.textContent = "Registrarme";

        slot.append(login, register);
    }
}

function requireSession(redirectTo) {
    const session = getSession();
    if (!session) {
        const next = encodeURIComponent(redirectTo || window.location.href);
        window.location.href = `registro-paciente.html?tab=registro&next=${next}`;
        return null;
    }
    return session;
}

/* ---------------------------------------------------------------------
   Página: index.html
--------------------------------------------------------------------- */
function initIndex() {
    renderHeaderAuth();
}

/* ---------------------------------------------------------------------
   Página: especialidades.html
--------------------------------------------------------------------- */
function initEspecialidades() {
    renderHeaderAuth();
    const container = $("#specialty-list");
    if (!container) return;

    SPECIALTIES.forEach((specialty) => {
        container.append(renderSpecialtyCard(specialty));
    });
}

function renderSpecialtyCard(specialty) {
    const card = document.createElement("article");
    card.className = "specialty-card";

    const head = document.createElement("button");
    head.type = "button";
    head.className = "specialty-head";
    head.innerHTML = `<span><span class="specialty-icon">${specialty.icono}</span>${specialty.nombre}</span><span class="chev">▾</span>`;
    head.addEventListener("click", () => card.classList.toggle("open"));
    card.append(head);

    const desc = document.createElement("p");
    desc.className = "specialty-desc";
    desc.textContent = specialty.descripcion;
    card.append(desc);

    const list = document.createElement("ul");
    list.className = "procedure-list";

    specialty.procedimientos.forEach((proc) => {
        const li = document.createElement("li");
        li.className = "procedure-item";

        const info = document.createElement("div");
        info.className = "procedure-info";
        const strong = document.createElement("strong");
        strong.textContent = proc.nombre;
        const meta = document.createElement("div");
        meta.className = "procedure-meta";
        meta.textContent = `${proc.descripcion} · Duración aprox. ${proc.duracionMin} min`;
        info.append(strong, meta);

        const actions = document.createElement("div");
        actions.className = "procedure-actions";
        const price = document.createElement("span");
        price.className = "procedure-price";
        price.textContent = `$${proc.precio} MXN`;
        const btn = document.createElement("a");
        btn.className = "btn btn-primary";
        btn.textContent = "Agendar";
        btn.href = `reservar-cita.html?especialidad=${specialty.id}&procedimiento=${proc.id}`;
        actions.append(price, btn);

        li.append(info, actions);
        list.append(li);
    });

    card.append(list);
    return card;
}

/* ---------------------------------------------------------------------
   Página: reservar-cita.html
--------------------------------------------------------------------- */
async function initReservarCita() {
    renderHeaderAuth();

    const params = new URLSearchParams(window.location.search);
    const specialtyId = params.get("especialidad") || "general";
    const procedureId = params.get("procedimiento");
    const found = procedureId ? findProcedure(specialtyId, procedureId) : null;

    if (!found) {
        $("#booking-app").innerHTML =
            '<p class="message error">No encontramos el procedimiento solicitado. Regresa a <a href="especialidades.html">especialidades</a>.</p>';
        return;
    }

    const session = requireSession(window.location.href);
    if (!session) return;

    const { specialty, procedure } = found;

    $("#booking-title").textContent = `${specialty.nombre} · ${procedure.nombre}`;
    $("#booking-desc").textContent = `${procedure.descripcion} Duración aprox. ${procedure.duracionMin} minutos.`;

    const state = { date: null, slotMin: null };

    const dateGrid = $("#date-grid");
    const dates = nextAvailableDates(10);
    const dow = ["dom", "lun", "mar", "mié", "jué", "vie", "sáb"];

    dates.forEach((date) => {
        const chip = document.createElement("button");
        chip.type = "button";
        chip.className = "date-chip";
        chip.innerHTML = `<span class="dow">${dow[date.getDay()]}</span><span class="num">${date.getDate()}</span>`;
        chip.addEventListener("click", () => {
            $all(".date-chip", dateGrid).forEach((c) => c.classList.remove("active"));
            chip.classList.add("active");
            state.date = date;
            state.slotMin = null;
            void renderSlots();
        });
        dateGrid.append(chip);
    });

    if (dates.length) {
        dateGrid.firstElementChild.click();
    }

    async function renderSlots() {
        const slotGrid = $("#slot-grid");
        slotGrid.innerHTML = "";
        $("#slot-empty").classList.add("hidden");
        $("#continue-btn").disabled = true;

        if (!state.date) return;
        let slots;
        try {
            slots = await generateSlots(state.date, procedure.id, specialty.id);
        } catch (error) {
            $("#slot-empty").textContent = error.message;
            $("#slot-empty").classList.remove("hidden");
            return;
        }

        if (!slots.length) {
            $("#slot-empty").classList.remove("hidden");
            return;
        }

        slots.forEach((min) => {
            const chip = document.createElement("button");
            chip.type = "button";
            chip.className = "slot-chip";
            chip.textContent = minutesToLabel(min);
            chip.addEventListener("click", () => {
                $all(".slot-chip", slotGrid).forEach((c) => c.classList.remove("active"));
                chip.classList.add("active");
                state.slotMin = min;
                $("#continue-btn").disabled = false;
            });
            slotGrid.append(chip);
        });
    }

    $("#booking-form").addEventListener("submit", (event) => {
        event.preventDefault();
        if (!state.date || state.slotMin === null) return;

        const draft = {
            specialtyId: specialty.id,
            specialtyName: specialty.nombre,
            procedureId: procedure.id,
            procedureName: procedure.nombre,
            precio: procedure.precio,
            duracionMin: procedure.duracionMin,
            fecha: dateKey(state.date),
            inicioMin: state.slotMin,
            finMin: state.slotMin + procedure.duracionMin,
            motivo: $("#motivo").value.trim()
        };
        sessionStorage.setItem("sf_draft_cita", JSON.stringify(draft));
        window.location.href = "pago-cita.html";
    });
}

/* ---------------------------------------------------------------------
   Página: pago-cita.html
--------------------------------------------------------------------- */
function initPagoCita() {
    renderHeaderAuth();
    const session = requireSession("pago-cita.html");
    if (!session) return;

    let draft;
    try {
        draft = JSON.parse(sessionStorage.getItem("sf_draft_cita"));
    } catch (e) {
        draft = null;
    }

    if (!draft) {
        $("#pago-app").innerHTML =
            '<p class="message error">No hay ninguna cita en proceso. Regresa a <a href="especialidades.html">especialidades</a>.</p>';
        return;
    }

    const date = new Date(`${draft.fecha}T00:00:00`);
    $("#resumen-especialidad").textContent = draft.specialtyName;
    $("#resumen-procedimiento").textContent = draft.procedureName;
    $("#resumen-fecha").textContent = date.toLocaleDateString("es-MX", {
        weekday: "long",
        day: "numeric",
        month: "long"
    });
    $("#resumen-hora").textContent = `${minutesToLabel(draft.inicioMin)} - ${minutesToLabel(draft.finMin)}`;
    $("#resumen-total").textContent = `$${draft.precio} MXN`;

    $("#pago-form").addEventListener("submit", async (event) => {
        event.preventDefault();
        const msg = $("#pago-message");
        msg.textContent = "";
        msg.className = "message";

        // La API crea un pago simulado. Una pasarela real debe reemplazar
        // este paso y confirmar la cita únicamente tras aprobar el cargo.

        try {
            const booking = await api("/citas", {
                method: "POST",
                body: JSON.stringify({
                    especialidadId: draft.specialtyId,
                    procedimientoId: draft.procedureId,
                    fecha: draft.fecha,
                    inicioMin: draft.inicioMin,
                    motivo: draft.motivo
                })
            });
            sessionStorage.removeItem("sf_draft_cita");
            window.location.href = `confirmacion-cita.html?id=${booking.id}`;
        } catch (error) {
            msg.textContent = error.message;
            msg.classList.add("error");
        }
    });
}

/* ---------------------------------------------------------------------
   Página: confirmacion-cita.html
--------------------------------------------------------------------- */
async function initConfirmacion() {
    renderHeaderAuth();
    const session = requireSession("confirmacion-cita.html");
    if (!session) return;
    const params = new URLSearchParams(window.location.search);
    const id = params.get("id");
    if (!id) {
        $("#confirm-app").innerHTML =
            '<p class="message error">No encontramos esa cita. Revisa <a href="bienvenida-paciente.html">mis citas</a>.</p>';
        return;
    }
    let booking;
    try {
        booking = await api(`/citas/${encodeURIComponent(id)}`);
    } catch (error) {
        $("#confirm-app").innerHTML =
            '<p class="message error">No encontramos esa cita. Revisa <a href="bienvenida-paciente.html">mis citas</a>.</p>';
        return;
    }

    const date = new Date(`${booking.fecha}T00:00:00`);
    $("#res-especialidad").textContent = booking.especialidad;
    $("#res-procedimiento").textContent = booking.procedimiento;
    $("#res-fecha").textContent = date.toLocaleDateString("es-MX", {
        weekday: "long",
        day: "numeric",
        month: "long"
    });
    $("#res-hora").textContent = `${minutesToLabel(booking.inicioMin)} - ${minutesToLabel(booking.finMin)}`;
    $("#res-total").textContent = `$${booking.precio} MXN`;
    $("#res-motivo").textContent = booking.motivo || "Sin comentarios";
}

/* ---------------------------------------------------------------------
   Página: bienvenida-paciente.html
--------------------------------------------------------------------- */
async function initBienvenida() {
    renderHeaderAuth();
    const session = requireSession("bienvenida-paciente.html");
    if (!session) return;

    $("#welcome-name").textContent = session.nombres;

    const list = $("#appointments-list");
    const empty = $("#appointments-empty");
    let bookings;
    try {
        bookings = await api("/citas/mias");
    } catch (error) {
        empty.textContent = error.message;
        empty.classList.remove("hidden");
        return;
    }

    if (!bookings.length) {
        empty.classList.remove("hidden");
        return;
    }

    bookings.forEach((booking) => {
        const date = new Date(`${booking.fecha}T00:00:00`);
        const card = document.createElement("div");
        card.className = "appointment-card";
        const content = document.createElement("div");
        const title = document.createElement("strong");
        title.textContent = `${booking.especialidad} · ${booking.procedimiento}`;
        const details = document.createElement("div");
        details.className = "procedure-meta";
        details.textContent = `${date.toLocaleDateString("es-MX", { weekday: "long", day: "numeric", month: "long" })} · ${minutesToLabel(booking.inicioMin)}`;
        content.append(title, details);
        const badge = document.createElement("span");
        badge.className = "badge badge-confirmed";
        badge.textContent = booking.estado;
        card.append(content, badge);
        list.append(card);
    });
}

/* ---------------------------------------------------------------------
   Página: perfil-paciente.html
--------------------------------------------------------------------- */
function initPerfil() {
    renderHeaderAuth();
    const session = requireSession("perfil-paciente.html");
    if (!session) return;

    $("#profile-nombres").value = session.nombres || "";
    $("#profile-paterno").value = session.apellidoPaterno || "";
    $("#profile-materno").value = session.apellidoMaterno || "";
    $("#profile-edad").value = session.edad || "";
    $("#profile-correo").value = session.correo || "";

    $("#profile-form").addEventListener("submit", async (event) => {
        event.preventDefault();
        const msg = $("#profile-message");
        try {
            const user = await api("/pacientes/me", {
                method: "PUT",
                body: JSON.stringify({
                    nombres: $("#profile-nombres").value.trim(),
                    apellidoPaterno: $("#profile-paterno").value.trim(),
                    apellidoMaterno: $("#profile-materno").value.trim(),
                    edad: Number($("#profile-edad").value)
                })
            });
            setSession(user);
            msg.textContent = "Tus datos se guardaron correctamente.";
            msg.className = "message ok";
            renderHeaderAuth();
        } catch (error) {
            msg.textContent = error.message;
            msg.className = "message error";
        }
    });
}

/* ---------------------------------------------------------------------
   Página: registro-paciente.html
--------------------------------------------------------------------- */
function initRegistro() {
    const params = new URLSearchParams(window.location.search);
    const tab = params.get("tab") === "registro" ? "register" : "login";
    const next = params.get("next");

    setAuthTab(tab);
    $all("[data-auth-tab]").forEach((btn) => {
        btn.addEventListener("click", () => setAuthTab(btn.dataset.authTab));
    });

    $("#login-form").addEventListener("submit", async (event) => {
        event.preventDefault();
        const msg = $("#auth-message");
        msg.textContent = "";
        msg.className = "message";
        try {
            await loginUser($("#login-correo").value.trim(), $("#login-password").value);
            window.location.href = resolveNext(next);
        } catch (err) {
            msg.textContent = err.message;
            msg.className = "message error";
        }
    });

    $("#register-form").addEventListener("submit", async (event) => {
        event.preventDefault();
        const msg = $("#auth-message");
        msg.textContent = "";
        msg.className = "message";
        try {
            await registerUser({
                nombres: $("#reg-nombres").value.trim(),
                apellidoPaterno: $("#reg-paterno").value.trim(),
                apellidoMaterno: $("#reg-materno").value.trim(),
                edad: $("#reg-edad").value,
                correo: $("#reg-correo").value.trim(),
                password: $("#reg-password").value
            });
            window.location.href = resolveNext(next);
        } catch (err) {
            msg.textContent = err.message;
            msg.className = "message error";
        }
    });
}

function resolveNext(next) {
    if (!next) return "bienvenida-paciente.html";
    try {
        const target = new URL(next, window.location.origin);
        return target.origin === window.location.origin ? `${target.pathname}${target.search}${target.hash}` : "bienvenida-paciente.html";
    } catch (error) {
        return "bienvenida-paciente.html";
    }
}

function setAuthTab(tab) {
    $all("[data-auth-tab]").forEach((btn) => btn.classList.toggle("active", btn.dataset.authTab === tab));
    $("#login-form").classList.toggle("hidden", tab !== "login");
    $("#register-form").classList.toggle("hidden", tab !== "register");
}

/* ---------------------------------------------------------------------
   Arranque
--------------------------------------------------------------------- */
document.addEventListener("DOMContentLoaded", async () => {
    await refreshSession();

    const page = document.body.dataset.page;
    const initByPage = {
        inicio: initIndex,
        especialidades: initEspecialidades,
        "reservar-cita": initReservarCita,
        "pago-cita": initPagoCita,
        confirmacion: initConfirmacion,
        bienvenida: initBienvenida,
        "perfil-paciente": initPerfil,
        registro: initRegistro
    };

    const init = initByPage[page];
    if (init) await init();
});
