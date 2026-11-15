const state = {
    students: [],
    selectedStudentId: null,
    subjects: [],
    assignments: []
};

// Инициализация при загрузке страницы
document.addEventListener("DOMContentLoaded", async () => {
    setDefaultDueDate();
    bindEvents();
    await loadStudents();
});

function setDefaultDueDate() {
    const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000);
    tomorrow.setMinutes(tomorrow.getMinutes() - tomorrow.getTimezoneOffset());
    document.getElementById("assignmentDueDate").value = tomorrow.toISOString().slice(0, 16);
}

function bindEvents() {
    document.getElementById("studentSelect").addEventListener("change", async (e) => {
        state.selectedStudentId = e.target.value ? Number(e.target.value) : null;
        await refreshStudentWorkspace();
    });

    document.getElementById("toggleStudentModalBtn").addEventListener("click", () => {
        document.getElementById("newStudentPanel").classList.toggle("hidden");
    });

    document.getElementById("closeStudentPanelBtn").addEventListener("click", () => {
        document.getElementById("newStudentPanel").classList.add("hidden");
    });

    document.getElementById("deleteStudentBtn").addEventListener("click", deleteCurrentStudent);
    document.getElementById("studentForm").addEventListener("submit", createStudent);
    document.getElementById("subjectForm").addEventListener("submit", createSubject);
    document.getElementById("assignmentForm").addEventListener("submit", createAssignment);

    document.getElementById("filterSubjectSelect").addEventListener("change", renderAssignments);
    document.getElementById("filterStatusSelect").addEventListener("change", renderAssignments);
}

// Универсальный помощник для HTTP-запросов с разбором ProblemDetails / ValidationProblemDetails
async function apiRequest(url, options = {}) {
    const response = await fetch(url, {
        headers: { "Content-Type": "application/json", ...(options.headers || {}) },
        ...options
    });

    if (response.status === 204) return null;

    const text = await response.text();
    const data = text ? JSON.parse(text) : null;

    if (!response.ok) {
        let message = data?.detail || data?.title || data?.message || `Ошибка HTTP ${response.status}`;
        if (data?.errors) {
            const validationErrors = Object.values(data.errors).flat().join(" ");
            if (validationErrors) message = validationErrors;
        }
        throw new Error(message);
    }

    return data;
}

function showToast(message, type = "success") {
    const toast = document.getElementById("toast");
    toast.textContent = message;
    toast.className = `toast ${type}`;
    clearTimeout(toast._timer);
    toast._timer = setTimeout(() => toast.classList.add("hidden"), 4500);
}

// --- СТУДЕНТЫ ---

async function loadStudents(preferId = null) {
    try {
        state.students = await apiRequest("/api/students");
        const select = document.getElementById("studentSelect");
        select.innerHTML = "";

        if (state.students.length === 0) {
            select.innerHTML = `<option value="">Нет студентов — создайте первого</option>`;
            state.selectedStudentId = null;
            document.getElementById("newStudentPanel").classList.remove("hidden");
            clearWorkspace();
            return;
        }

        state.students.forEach(s => {
            const opt = document.createElement("option");
            opt.value = s.id;
            opt.textContent = `${s.fullName} (${s.academicGroup})`;
            select.appendChild(opt);
        });

        const targetId = preferId && state.students.some(s => s.id === preferId)
            ? preferId
            : state.students[0].id;

        state.selectedStudentId = targetId;
        select.value = String(targetId);

        await refreshStudentWorkspace();
    } catch (err) {
        showToast(err.message, "error");
    }
}

async function createStudent(e) {
    e.preventDefault();
    const payload = {
        fullName: document.getElementById("studentFullName").value.trim(),
        email: document.getElementById("studentEmail").value.trim(),
        academicGroup: document.getElementById("studentGroup").value.trim()
    };

    try {
        const created = await apiRequest("/api/students", {
            method: "POST",
            body: JSON.stringify(payload)
        });
        e.target.reset();
        document.getElementById("newStudentPanel").classList.add("hidden");
        showToast(`Студент ${created.fullName} добавлен`);
        await loadStudents(created.id);
    } catch (err) {
        showToast(err.message, "error");
    }
}

async function deleteCurrentStudent() {
    if (!state.selectedStudentId) return;
    if (!confirm("Удалить выбранного студента вместе со всеми его предметами и заданиями?")) return;

    try {
        await apiRequest(`/api/students/${state.selectedStudentId}`, { method: "DELETE" });
        showToast("Студент удалён");
        await loadStudents();
    } catch (err) {
        showToast(err.message, "error");
    }
}

// --- ОБНОВЛЕНИЕ ДАННЫХ ПО СТУДЕНТУ ---

async function refreshStudentWorkspace() {
    if (!state.selectedStudentId) {
        clearWorkspace();
        return;
    }

    await Promise.all([
        loadSubjects(),
        loadForecast()
    ]);
    await loadAssignments();
}

function clearWorkspace() {
    state.subjects = [];
    state.assignments = [];
    renderSubjects();
    renderAssignments();
    document.getElementById("forecastRiskBadge").textContent = "—";
    document.getElementById("forecastRiskBadge").className = "risk-badge risk-none";
    document.getElementById("forecastHours").textContent = "0";
    document.getElementById("forecastActiveCount").textContent = "0";
    document.getElementById("forecastRecommendation").textContent = "Выберите или создайте студента для расчёта прогноза.";
}

// --- ПРОГНОЗ НАГРУЗКИ ---

async function loadForecast() {
    try {
        const forecast = await apiRequest(`/api/workload/forecast/${state.selectedStudentId}`);
        const badge = document.getElementById("forecastRiskBadge");
        const level = (forecast.riskLevel || "Low").toLowerCase();

        badge.textContent = forecast.riskLevel || "Low";
        badge.className = `risk-badge risk-${level}`;

        document.getElementById("forecastHours").textContent = Number(forecast.totalPendingHours || 0).toFixed(1);
        document.getElementById("forecastActiveCount").textContent = forecast.activeAssignmentsCount ?? 0;
        document.getElementById("forecastRecommendation").textContent = forecast.recommendation || "Нагрузка в пределах нормы.";
    } catch (err) {
        showToast(`Не удалось рассчитать прогноз: ${err.message}`, "error");
    }
}

// --- ПРЕДМЕТЫ ---

async function loadSubjects() {
    try {
        state.subjects = await apiRequest(`/api/subjects?studentId=${state.selectedStudentId}`);
        renderSubjects();
    } catch (err) {
        showToast(err.message, "error");
    }
}

function renderSubjects() {
    const list = document.getElementById("subjectsList");
    const assignmentSubject = document.getElementById("assignmentSubject");
    const filterSubject = document.getElementById("filterSubjectSelect");

    list.innerHTML = "";
    assignmentSubject.innerHTML = "";
    filterSubject.innerHTML = `<option value="all">Все предметы</option>`;

    if (state.subjects.length === 0) {
        list.innerHTML = `<li class="empty-state">У студента пока нет предметов</li>`;
        assignmentSubject.innerHTML = `<option value="">Сначала добавьте предмет</option>`;
        return;
    }

    state.subjects.forEach(subj => {
        // Элемент списка
        const li = document.createElement("li");
        li.className = "subject-item";
        li.innerHTML = `
            <div>
                <div class="subject-title">${escapeHtml(subj.name)}</div>
                <div class="subject-meta">Преподаватель: ${escapeHtml(subj.teacherName)} • ${subj.totalCredits} ECTS</div>
            </div>
            <button class="btn btn-danger-outline btn-sm" onclick="deleteSubject(${subj.id})">Удалить</button>
        `;
        list.appendChild(li);

        // Селекты формы и фильтра
        const opt1 = new Option(subj.name, subj.id);
        const opt2 = new Option(subj.name, subj.id);
        assignmentSubject.appendChild(opt1);
        filterSubject.appendChild(opt2);
    });
}

async function createSubject(e) {
    e.preventDefault();
    if (!state.selectedStudentId) {
        showToast("Сначала выберите или создайте студента", "error");
        return;
    }

    const payload = {
        studentId: state.selectedStudentId,
        name: document.getElementById("subjectName").value.trim(),
        teacherName: document.getElementById("subjectTeacher").value.trim(),
        totalCredits: parseInt(document.getElementById("subjectCredits").value, 10)
    };
    try {
        await apiRequest("/api/subjects", {
            method: "POST",
            body: JSON.stringify(payload)
        });
        e.target.reset();
        document.getElementById("subjectCredits").value = "4";
        showToast("Предмет добавлен");
        await loadSubjects();
    } catch (err) {
        showToast(err.message, "error");
    }
}

async function deleteSubject(subjectId) {
    if (!confirm("Удалить предмет и связанные с ним задания?")) return;
    try {
        await apiRequest(`/api/subjects/${subjectId}`, { method: "DELETE" });
        showToast("Предмет удалён");
        await refreshStudentWorkspace();
    } catch (err) {
        showToast(err.message, "error");
    }
}

// --- ЗАДАНИЯ ---

async function loadAssignments() {
    try {
        const all = await apiRequest("/api/assignments");
        const studentSubjectIds = new Set(state.subjects.map(s => s.id));
        state.assignments = all.filter(a => studentSubjectIds.has(a.subjectId));
        renderAssignments();
    } catch (err) {
        showToast(err.message, "error");
    }
}

function renderAssignments() {
    const container = document.getElementById("assignmentsList");
    const subjectFilter = document.getElementById("filterSubjectSelect").value;
    const statusFilter = document.getElementById("filterStatusSelect").value;

    const subjectMap = new Map(state.subjects.map(s => [s.id, s.name]));

    let filtered = [...state.assignments];
    if (subjectFilter !== "all") {
        filtered = filtered.filter(a => a.subjectId === Number(subjectFilter));
    }
    if (statusFilter !== "all") {
        const targetCompleted = statusFilter === "true";
        filtered = filtered.filter(a => a.isCompleted === targetCompleted);
    }

    container.innerHTML = "";
    if (filtered.length === 0) {
        container.innerHTML = `<div class="empty-state">Заданий по выбранным фильтрам не найдено</div>`;
        return;
    }

    filtered.forEach(a => {
        const dueDate = new Date(a.dueDate).toLocaleString("ru-RU", {
            day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit"
        });
        const subjectName = subjectMap.get(a.subjectId) || `Предмет #${a.subjectId}`;

        const card = document.createElement("div");
        card.className = `assignment-card ${a.isCompleted ? "completed" : ""}`;
        card.innerHTML = `
            <div>
                <div class="assignment-title">${escapeHtml(a.title)}</div>
                <div class="assignment-desc">${escapeHtml(a.description || "")}</div>
                <div class="assignment-badges">
                    <span class="badge">${escapeHtml(subjectName)}</span>
                    <span class="badge badge-hours">${a.estimatedHours} ч.</span>
                    <span class="badge">Дедлайн: ${dueDate}</span>
                </div>
            </div>
            <div class="assignment-actions">
                ${!a.isCompleted ? `<button class="btn btn-success btn-sm" onclick="completeAssignment(${a.id})">Выполнено</button>` : ""}
                <button class="btn btn-danger-outline btn-sm" onclick="deleteAssignment(${a.id})">Удалить</button>
            </div>
        `;
        container.appendChild(card);
    });
}

async function createAssignment(e) {
    e.preventDefault();
    const subjectId = Number(document.getElementById("assignmentSubject").value);
    if (!subjectId) {
        showToast("Выберите предмет для задания", "error");
        return;
    }

    const rawDate = document.getElementById("assignmentDueDate").value;
    const payload = {
        subjectId,
        title: document.getElementById("assignmentTitle").value.trim(),
        description: document.getElementById("assignmentDescription").value.trim(),
        dueDate: new Date(rawDate).toISOString(),
        estimatedHours: Number(document.getElementById("assignmentHours").value)
    };

    try {
        await apiRequest("/api/assignments", {
            method: "POST",
            body: JSON.stringify(payload)
        });
        document.getElementById("assignmentTitle").value = "";
        document.getElementById("assignmentDescription").value = "";
        showToast("Задание добавлено, прогноз пересчитан");
        await refreshStudentWorkspace();
    } catch (err) {
        showToast(err.message, "error");
    }
}

async function completeAssignment(id) {
    try {
        await apiRequest(`/api/assignments/${id}/complete`, { method: "PATCH" });
        showToast("Задание отмечено выполненным");
        await refreshStudentWorkspace();
    } catch (err) {
        showToast(err.message, "error");
    }
}

async function deleteAssignment(id) {
    if (!confirm("Удалить это задание?")) return;
    try {
        await apiRequest(`/api/assignments/${id}`, { method: "DELETE" });
        showToast("Задание удалено");
        await refreshStudentWorkspace();
    } catch (err) {
        showToast(err.message, "error");
    }
}

function escapeHtml(str) {
    return String(str)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}