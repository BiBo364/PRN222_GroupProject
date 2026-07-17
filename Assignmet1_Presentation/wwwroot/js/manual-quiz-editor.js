(() => {
    "use strict";

    const editor = document.getElementById("manualQuizEditor");
    if (!editor) return;

    const form = document.getElementById("manualQuizForm");
    const list = document.getElementById("manualQuestionList");
    const countLabel = document.getElementById("manualQuestionCount");
    const saveState = document.getElementById("manualSaveState");
    const saveText = document.getElementById("manualSaveText");
    const initialNode = document.getElementById("manualQuizInitialData");
    const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";

    let storageKey = editor.dataset.storageKey;
    let autosaveTimer = null;
    let serverSaveTimer = null;
    let serverSaveInFlight = false;
    let pendingServerSave = false;
    let hasUnsavedChanges = false;

    const parseJson = (value, fallback) => {
        try {
            return JSON.parse(value);
        } catch {
            return fallback;
        }
    };

    const initialData = parseJson(initialNode?.textContent ?? "{}", {});

    const escapeHtml = value => String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");

    const newClientKey = () => {
        if (window.crypto?.randomUUID) return window.crypto.randomUUID().replaceAll("-", "");
        return `question-${Date.now()}-${Math.random().toString(16).slice(2)}`;
    };

    const defaultQuestion = () => ({
        id: null,
        clientKey: newClientKey(),
        questionType: "multiple_choice",
        prompt: "",
        options: ["", "", "", ""],
        correctAnswer: "",
        explanation: "",
        difficulty: "medium",
        topic: "",
        points: 1
    });

    const normalizeQuestion = source => {
        const question = { ...defaultQuestion(), ...(source ?? {}) };
        question.clientKey ||= newClientKey();
        question.options = Array.isArray(question.options) ? question.options : [];
        if (question.questionType === "multiple_choice") {
            while (question.options.length < 4) question.options.push("");
            question.options = question.options.slice(0, 6);
        }
        return question;
    };

    const setSaveState = (type, message) => {
        saveState.classList.remove("is-saving", "is-saved", "is-offline", "is-error");
        if (type) saveState.classList.add(type);
        saveText.textContent = message;
    };

    const updateQuestionCount = () => {
        countLabel.textContent = list.querySelectorAll(".manual-question-card").length.toString();
    };

    const answerMarkup = question => {
        if (question.questionType === "true_false") {
            return `
                <div class="form-field manual-answer-field">
                    <label>Đáp án đúng</label>
                    <select class="form-select manual-true-false-answer">
                        <option value="">Chọn đáp án</option>
                        <option value="Đúng" ${question.correctAnswer === "Đúng" ? "selected" : ""}>Đúng</option>
                        <option value="Sai" ${question.correctAnswer === "Sai" ? "selected" : ""}>Sai</option>
                    </select>
                </div>`;
        }

        if (question.questionType === "short_answer") {
            return `
                <div class="form-field manual-answer-field">
                    <label>Đáp án mẫu</label>
                    <input class="form-control manual-short-answer"
                           value="${escapeHtml(question.correctAnswer)}"
                           placeholder="Nhập đáp án chính xác." />
                </div>`;
        }

        const selectedIndex = question.options.findIndex(option =>
            option.trim().toLocaleLowerCase("vi") ===
            String(question.correctAnswer ?? "").trim().toLocaleLowerCase("vi"));

        return `
            <div class="manual-option-list">
                ${question.options.map((option, index) => `
                    <div class="manual-option-row">
                        <label class="manual-correct-picker" title="Đặt làm đáp án đúng">
                            <input type="radio"
                                   class="manual-correct-option"
                                   name="correct-${escapeHtml(question.clientKey)}"
                                   value="${index}"
                                   ${selectedIndex === index ? "checked" : ""} />
                            <span>${String.fromCharCode(65 + index)}</span>
                        </label>
                        <input class="form-control manual-option-input"
                               value="${escapeHtml(option)}"
                               placeholder="Phương án ${index + 1}" />
                        ${question.options.length > 2 ? `
                            <button type="button" class="manual-option-remove" title="Xóa phương án">
                                <i data-lucide="x" class="icon-xs"></i>
                            </button>` : ""}
                    </div>`).join("")}
                <button type="button"
                        class="manual-option-add"
                        ${question.options.length >= 6 ? "disabled" : ""}>
                    <i data-lucide="plus" class="icon-xs"></i>
                    Thêm phương án
                </button>
            </div>`;
    };

    const questionMarkup = question => `
        <article class="surface-card manual-question-card"
                 data-question-id="${question.id ?? ""}"
                 data-client-key="${escapeHtml(question.clientKey)}">
            <div class="manual-question-card-header">
                <div class="manual-question-order">
                    <span class="manual-question-index">01</span>
                    <span>Câu hỏi</span>
                </div>
                <div class="manual-question-card-actions">
                    <button type="button" class="manual-question-move-up" title="Chuyển lên">
                        <i data-lucide="arrow-up" class="icon-sm"></i>
                    </button>
                    <button type="button" class="manual-question-move-down" title="Chuyển xuống">
                        <i data-lucide="arrow-down" class="icon-sm"></i>
                    </button>
                    <button type="button" class="manual-question-duplicate" title="Nhân bản câu hỏi">
                        <i data-lucide="copy" class="icon-sm"></i>
                    </button>
                    <button type="button" class="manual-question-delete" title="Xóa câu hỏi">
                        <i data-lucide="trash-2" class="icon-sm"></i>
                    </button>
                </div>
            </div>
            <div class="manual-question-body">
                <div class="manual-question-config">
                    <div class="form-field">
                        <label>Loại câu hỏi</label>
                        <select class="form-select manual-question-type">
                            <option value="multiple_choice" ${question.questionType === "multiple_choice" ? "selected" : ""}>Nhiều lựa chọn</option>
                            <option value="true_false" ${question.questionType === "true_false" ? "selected" : ""}>Đúng hoặc sai</option>
                            <option value="short_answer" ${question.questionType === "short_answer" ? "selected" : ""}>Trả lời ngắn</option>
                        </select>
                    </div>
                    <div class="form-field">
                        <label>Mức độ</label>
                        <select class="form-select manual-question-difficulty">
                            <option value="easy" ${question.difficulty === "easy" ? "selected" : ""}>Cơ bản</option>
                            <option value="medium" ${question.difficulty === "medium" ? "selected" : ""}>Trung bình</option>
                            <option value="hard" ${question.difficulty === "hard" ? "selected" : ""}>Nâng cao</option>
                        </select>
                    </div>
                    <div class="form-field">
                        <label>Điểm</label>
                        <input type="number"
                               class="form-control manual-question-points"
                               min="0.25"
                               max="100"
                               step="0.25"
                               value="${escapeHtml(question.points)}" />
                    </div>
                </div>
                <div class="form-field">
                    <label>Nội dung câu hỏi</label>
                    <textarea class="form-control manual-question-prompt"
                              rows="3"
                              placeholder="Nhập câu hỏi rõ ràng, chỉ tập trung vào một kiến thức.">${escapeHtml(question.prompt)}</textarea>
                </div>
                <div class="manual-question-answer-area">
                    ${answerMarkup(question)}
                </div>
                <details class="manual-question-more" ${question.explanation || question.topic ? "open" : ""}>
                    <summary>Giải thích và phân loại</summary>
                    <div class="form-grid">
                        <div class="form-field">
                            <label>Giải thích đáp án</label>
                            <textarea class="form-control manual-question-explanation"
                                      rows="3"
                                      placeholder="Giúp sinh viên hiểu vì sao đáp án đúng.">${escapeHtml(question.explanation)}</textarea>
                        </div>
                        <div class="form-field">
                            <label>Chủ đề</label>
                            <input class="form-control manual-question-topic"
                                   value="${escapeHtml(question.topic)}"
                                   maxlength="200"
                                   placeholder="Ví dụ: Kế thừa, LINQ, Dependency Injection." />
                        </div>
                    </div>
                </details>
            </div>
        </article>`;

    const refreshIcons = () => window.lucide?.createIcons?.();

    const appendQuestion = (source, afterCard = null) => {
        const question = normalizeQuestion(source);
        const wrapper = document.createElement("div");
        wrapper.innerHTML = questionMarkup(question).trim();
        const card = wrapper.firstElementChild;
        if (afterCard) afterCard.insertAdjacentElement("afterend", card);
        else list.append(card);
        reindexQuestions();
        refreshIcons();
        return card;
    };

    const gatherQuestion = card => {
        const type = card.querySelector(".manual-question-type").value;
        const options = type === "multiple_choice"
            ? [...card.querySelectorAll(".manual-option-input")].map(input => input.value)
            : type === "true_false" ? ["Đúng", "Sai"] : [];
        let correctAnswer = "";
        if (type === "multiple_choice") {
            const selected = card.querySelector(".manual-correct-option:checked");
            if (selected) correctAnswer = options[Number(selected.value)] ?? "";
        } else if (type === "true_false") {
            correctAnswer = card.querySelector(".manual-true-false-answer")?.value ?? "";
        } else {
            correctAnswer = card.querySelector(".manual-short-answer")?.value ?? "";
        }

        return {
            id: card.dataset.questionId ? Number(card.dataset.questionId) : null,
            clientKey: card.dataset.clientKey,
            questionType: type,
            prompt: card.querySelector(".manual-question-prompt").value,
            options,
            correctAnswer,
            explanation: card.querySelector(".manual-question-explanation").value,
            difficulty: card.querySelector(".manual-question-difficulty").value,
            topic: card.querySelector(".manual-question-topic").value,
            points: Number(card.querySelector(".manual-question-points").value || 1)
        };
    };

    const gatherData = () => ({
        id: Number(document.getElementById("manualQuizId").value) || null,
        title: form.querySelector('[name="Input.Title"]').value,
        description: form.querySelector('[name="Input.Description"]').value,
        instructions: form.querySelector('[name="Input.Instructions"]').value,
        durationMinutes: Number(form.querySelector('[name="Input.DurationMinutes"]').value || 15),
        isPublished: document.getElementById("manualQuizPublished").value.toLowerCase() === "true",
        shuffleQuestions: form.querySelector('[name="Input.ShuffleQuestions"]').checked,
        shuffleOptions: form.querySelector('[name="Input.ShuffleOptions"]').checked,
        questions: [...list.querySelectorAll(".manual-question-card")].map(gatherQuestion)
    });

    const applyData = data => {
        form.querySelector('[name="Input.Title"]').value = data.title ?? "";
        form.querySelector('[name="Input.Description"]').value = data.description ?? "";
        form.querySelector('[name="Input.Instructions"]').value = data.instructions ?? "";
        form.querySelector('[name="Input.DurationMinutes"]').value = data.durationMinutes ?? 15;
        form.querySelector('[name="Input.ShuffleQuestions"]').checked = data.shuffleQuestions !== false;
        form.querySelector('[name="Input.ShuffleOptions"]').checked = data.shuffleOptions !== false;
        list.innerHTML = "";
        const questions = Array.isArray(data.questions) && data.questions.length > 0
            ? data.questions
            : [defaultQuestion()];
        questions.forEach(question => appendQuestion(question));
        reindexQuestions();
    };

    function reindexQuestions() {
        [...list.querySelectorAll(".manual-question-card")].forEach((card, index) => {
            const question = gatherQuestion(card);
            card.querySelector(".manual-question-index").textContent = String(index + 1).padStart(2, "0");
            card.querySelector(".manual-question-move-up").disabled = index === 0;
            card.querySelector(".manual-question-move-down").disabled =
                index === list.querySelectorAll(".manual-question-card").length - 1;

            const fields = {
                Id: question.id ?? "",
                ClientKey: question.clientKey,
                QuestionType: question.questionType,
                Prompt: question.prompt,
                CorrectAnswer: question.correctAnswer,
                Explanation: question.explanation,
                Difficulty: question.difficulty,
                Topic: question.topic,
                Points: question.points
            };
            card.querySelectorAll(".manual-bound-hidden").forEach(input => input.remove());
            Object.entries(fields).forEach(([name, value]) => {
                const hidden = document.createElement("input");
                hidden.type = "hidden";
                hidden.className = "manual-bound-hidden";
                hidden.name = `Input.Questions[${index}].${name}`;
                hidden.value = value ?? "";
                card.append(hidden);
            });
            question.options.forEach((option, optionIndex) => {
                const hidden = document.createElement("input");
                hidden.type = "hidden";
                hidden.className = "manual-bound-hidden";
                hidden.name = `Input.Questions[${index}].Options[${optionIndex}]`;
                hidden.value = option;
                card.append(hidden);
            });
        });
        updateQuestionCount();
    }

    const saveLocalDraft = () => {
        reindexQuestions();
        const envelope = {
            savedAt: new Date().toISOString(),
            data: gatherData()
        };
        try {
            localStorage.setItem(storageKey, JSON.stringify(envelope));
            if (!navigator.onLine) {
                setSaveState("is-offline", "Đang ngoại tuyến — tiến trình đã lưu trên thiết bị.");
            } else {
                setSaveState("is-saved", `Đã lưu trên thiết bị lúc ${new Date().toLocaleTimeString("vi-VN")}.`);
            }
        } catch {
            setSaveState("is-error", "Không thể lưu trên thiết bị. Hãy nhấn “Lưu bản nháp”.");
        }
    };

    const scheduleSave = () => {
        clearTimeout(autosaveTimer);
        clearTimeout(serverSaveTimer);
        hasUnsavedChanges = true;
        setSaveState("is-saving", "Đang ghi nhận thay đổi...");
        autosaveTimer = setTimeout(saveLocalDraft, 350);
        serverSaveTimer = setTimeout(saveServerDraft, 2200);
    };

    const saveServerDraft = async () => {
        if (!navigator.onLine) {
            setSaveState("is-offline", "Đang ngoại tuyến — tiến trình đã lưu trên thiết bị.");
            return;
        }
        if (serverSaveInFlight) {
            pendingServerSave = true;
            return;
        }

        const data = gatherData();
        const hasMeaningfulContent = data.title.trim().length > 0
            || data.questions.some(question =>
                question.prompt.trim().length > 0
                || question.options.some(option => option.trim().length > 0));
        if (!hasMeaningfulContent) return;

        serverSaveInFlight = true;
        setSaveState("is-saving", "Đang đồng bộ bản nháp lên máy chủ...");
        try {
            const response = await fetch(editor.dataset.autosaveUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify(data)
            });
            const result = await response.json();
            if (!response.ok || !result.success) {
                throw new Error(result.message || "Không thể đồng bộ bản nháp.");
            }

            if (!data.id && result.quizId) {
                const oldKey = storageKey;
                storageKey = oldKey.replace(/:new$/, `:${result.quizId}`);
                editor.dataset.storageKey = storageKey;
                const cached = localStorage.getItem(oldKey);
                if (cached) localStorage.setItem(storageKey, cached);
                localStorage.removeItem(oldKey);
                document.getElementById("manualQuizId").value = result.quizId;
                editor.dataset.autosaveUrl = `${result.editUrl}?handler=Autosave`;
                window.history.replaceState({}, "", result.editUrl);
            }

            Object.entries(result.questionIds ?? {}).forEach(([clientKey, questionId]) => {
                const card = [...list.querySelectorAll(".manual-question-card")]
                    .find(candidate => candidate.dataset.clientKey === clientKey);
                if (card) card.dataset.questionId = questionId;
            });
            reindexQuestions();

            document.getElementById("manualQuizPublished").value =
                result.isPublished ? "true" : "false";
            hasUnsavedChanges = false;
            try {
                localStorage.setItem(storageKey, JSON.stringify({
                    savedAt: new Date(result.savedAt).toISOString(),
                    data: gatherData()
                }));
            } catch {
                // Bản nháp đã được lưu trên máy chủ; lỗi bộ nhớ trình duyệt không làm mất dữ liệu.
            }
            setSaveState("is-saved", `Đã đồng bộ lúc ${new Date(result.savedAt).toLocaleTimeString("vi-VN")}.`);
        } catch {
            setSaveState(
                "is-error",
                "Chưa thể đồng bộ máy chủ — bản gần nhất vẫn an toàn trên thiết bị."
            );
        } finally {
            serverSaveInFlight = false;
            if (pendingServerSave) {
                pendingServerSave = false;
                serverSaveTimer = setTimeout(saveServerDraft, 500);
            }
        }
    };

    const restoreNewestDraft = () => {
        const cached = parseJson(localStorage.getItem(storageKey) ?? "", null);
        const localSavedAt = cached?.savedAt ? Date.parse(cached.savedAt) : 0;
        const serverSavedAt = editor.dataset.serverUpdatedAt
            ? Date.parse(editor.dataset.serverUpdatedAt)
            : 0;
        if (cached?.data && localSavedAt > serverSavedAt) {
            applyData(cached.data);
            hasUnsavedChanges = true;
            setSaveState(
                "is-saved",
                `Đã khôi phục tiến trình lưu lúc ${new Date(cached.savedAt).toLocaleString("vi-VN")}.`
            );
            if (navigator.onLine) {
                serverSaveTimer = setTimeout(saveServerDraft, 500);
            }
            return;
        }
        hasUnsavedChanges = false;
        applyData(initialData);
    };

    list.addEventListener("click", event => {
        const card = event.target.closest(".manual-question-card");
        if (!card) return;

        if (event.target.closest(".manual-question-delete")) {
            card.remove();
            if (!list.children.length) appendQuestion(defaultQuestion());
        } else if (event.target.closest(".manual-question-duplicate")) {
            const copy = gatherQuestion(card);
            copy.id = null;
            copy.clientKey = newClientKey();
            appendQuestion(copy, card);
        } else if (event.target.closest(".manual-question-move-up")) {
            const previous = card.previousElementSibling;
            if (previous) list.insertBefore(card, previous);
        } else if (event.target.closest(".manual-question-move-down")) {
            const next = card.nextElementSibling;
            if (next) list.insertBefore(next, card);
        } else if (event.target.closest(".manual-option-remove")) {
            const current = gatherQuestion(card);
            const optionRow = event.target.closest(".manual-option-row");
            const optionIndex = [...card.querySelectorAll(".manual-option-row")].indexOf(optionRow);
            current.options.splice(optionIndex, 1);
            const replacement = appendQuestion(current, card);
            card.remove();
            replacement.scrollIntoView({ block: "nearest" });
        } else if (event.target.closest(".manual-option-add")) {
            const current = gatherQuestion(card);
            current.options.push("");
            const replacement = appendQuestion(current, card);
            card.remove();
            replacement.querySelectorAll(".manual-option-input")[current.options.length - 1]?.focus();
        } else {
            return;
        }

        reindexQuestions();
        refreshIcons();
        scheduleSave();
    });

    list.addEventListener("change", event => {
        const typeSelect = event.target.closest(".manual-question-type");
        if (typeSelect) {
            const card = typeSelect.closest(".manual-question-card");
            const current = gatherQuestion(card);
            current.questionType = typeSelect.value;
            current.correctAnswer = "";
            const replacement = appendQuestion(current, card);
            card.remove();
            replacement.querySelector(".manual-question-prompt")?.focus();
        }
        scheduleSave();
    });

    list.addEventListener("input", scheduleSave);
    form.addEventListener("input", event => {
        if (!event.target.closest(".manual-question-card")) scheduleSave();
    });
    form.addEventListener("change", event => {
        if (!event.target.closest(".manual-question-card")) scheduleSave();
    });
    form.addEventListener("submit", () => {
        clearTimeout(autosaveTimer);
        clearTimeout(serverSaveTimer);
        saveLocalDraft();
        reindexQuestions();
        setSaveState("is-saving", "Đang lưu Quiz...");
    });

    const addQuestion = () => {
        const card = appendQuestion(defaultQuestion());
        card.querySelector(".manual-question-prompt")?.focus();
        card.scrollIntoView({ behavior: "smooth", block: "center" });
        scheduleSave();
    };
    document.getElementById("addManualQuestion").addEventListener("click", addQuestion);
    document.getElementById("addManualQuestionBottom").addEventListener("click", addQuestion);

    window.addEventListener("offline", () =>
        setSaveState("is-offline", "Đang ngoại tuyến — tiến trình đã lưu trên thiết bị."));
    window.addEventListener("online", () => {
        setSaveState("is-saving", "Đã có kết nối — đang đồng bộ bản nháp...");
        saveServerDraft();
    });
    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState === "hidden" && hasUnsavedChanges) {
            saveLocalDraft();
        }
    });

    restoreNewestDraft();
    reindexQuestions();
    refreshIcons();
})();
