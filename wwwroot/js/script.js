// ========== GLOBAL DEĞİŞKENLER ==========
let books = [];
let currentUser = null;

const statusText = {
    Available: "Available",
    Borrowed: "Borrowed",
    Reserved: "Reserved"
};

function getStatusClass(status) {
    if (status === "Available") return "status-available";
    if (status === "Borrowed") return "status-borrowed";
    if (status === "Reserved") return "status-reserved";
    return "";
}

/* ===========================
   SAYFA YÜKLENİNCE
   =========================== */
document.addEventListener("DOMContentLoaded", async () => {
    currentUser = await window.checkAuth();
    await fetchBooks();
    setupSearch();
    setupButtons();
    setupAddBookModal();
    setupNavLinks();
});

/* ===========================
   KİTAPLARI BACKEND'DEN YÜKLE
   =========================== */
async function fetchBooks() {
    try {
        const response = await fetch(`${window.API_BASE_URL}/books`, {
            credentials: 'include'
        });
        const data = await response.json();
        if (data.success && data.data) {
            books = data.data;
            renderBooks();
        }
    } catch (error) {
        console.error('Error loading books:', error);
        alert('Books could not be loaded');
    }
}

/* ===========================
   KİTAP KARTI OLUŞTUR
   =========================== */
function createBookCard(book) {
    const card = document.createElement("article");
    card.className = "book-card";
    card.dataset.category = book.category;
    card.dataset.status = book.status;
    card.dataset.bookId = book.bookId;

    const coverImage = book.coverImageUrl || 'https://via.placeholder.com/150x200?text=No+Cover';

    card.innerHTML = `
        <div class="book-tag">
            <span class="book-tag-dot"></span>
            <span>${escapeHtml(book.category || "")}</span>
        </div>
        <div class="book-cover" style="background-image: url('${escapeHtml(coverImage)}');"></div>
        <div class="book-title">${escapeHtml(book.title)}</div>
        <div class="book-author">${escapeHtml(book.author)}</div>
        <div class="book-meta-row">
            <div class="book-owner">📚 ${escapeHtml(book.ownerName)}</div>
            <div class="book-status ${getStatusClass(book.status)}">
                ${statusText[book.status]}
            </div>
        </div>
    `;

    card.addEventListener("click", () => openBookDetail(book));
    return card;
}

/* ===========================
   KİTAPLARI KATEGORİLERE GÖRE RENDER ET
   =========================== */
function renderBooks(filteredBooks = null) {
    const mapId = {
        "Engineering Faculty": "list-engineering",
        "Faculty of Science & Letters": "list-letters",
        "Medical Faculty": "list-medicine",
        "Pharmacy Faculty": "list-pharmacy",
        "Dentistry Faculty": "list-dentistry",
        "Health Sciences Faculty": "list-Health",
        "Law Faculty": "list-law",
        "Fine Arts / Music / Conservatory": "list-arts",
        "Psychology": "list-psychology",
        "Education Faculty": "list-education",
        "Economics / Business / Management Faculty": "list-business",
        "Communication Faculty": "list-communication",
        "Architecture & Design Faculty": "list-architecture",
        "English & Language Exams": "list-english",
    };

    Object.values(mapId).forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerHTML = "";
    });

    const source = filteredBooks || books;
    source.forEach(book => {
        const listId = mapId[book.category];
        if (!listId) return;
        const listEl = document.getElementById(listId);
        if (!listEl) return;
        listEl.appendChild(createBookCard(book));
    });
}

/* ===========================
   AI ÖNERİ BÖLÜMÜ — RENDER
   =========================== */
function renderAISuggestions(query, aiResult, matchedBooks) {
    const existing = document.getElementById("aiSuggestionsSection");
    if (existing) existing.remove();

    if (!aiResult || matchedBooks.length === 0) return;

    const section = document.createElement("section");
    section.id = "aiSuggestionsSection";
    section.className = "category";
    section.style.cssText = "margin-top: 24px; border: 1px solid rgba(45,212,255,0.3); border-radius: 18px; padding: 16px; background: rgba(45,212,255,0.05);";

    section.innerHTML = `
        <div class="category-header" style="margin-bottom: 14px;">
            <div class="category-title">
                <div class="category-dot" style="background: #2dd4ff;"></div>
                <div>
                    <div class="category-name">🤖 AI Suggestions for "${escapeHtml(query)}"</div>
                    <div class="category-sub">
                        Primary: <strong>${escapeHtml(aiResult.primaryCategory)}</strong>
                        ${aiResult.relatedCategories?.length ? `· Related: ${aiResult.relatedCategories.map(c => escapeHtml(c)).join(", ")}` : ""}
                    </div>
                </div>
            </div>
            <button onclick="clearAISuggestions()" style="background:transparent; border:1px solid rgba(148,163,184,0.4); color:#9ca3af; border-radius:999px; padding:4px 12px; cursor:pointer; font-size:12px;">✕ Clear</button>
        </div>
        <div class="book-list" id="ai-suggestions-list"></div>
    `;

    const sectionHeader = document.querySelector(".section-header");
    if (sectionHeader) {
        sectionHeader.insertAdjacentElement("afterend", section);
    }

    const list = document.getElementById("ai-suggestions-list");
    matchedBooks.slice(0, 8).forEach(book => {
        list.appendChild(createBookCard(book));
    });
}

function clearAISuggestions() {
    const section = document.getElementById("aiSuggestionsSection");
    if (section) section.remove();
}

/* ===========================
   ARAMA FONKSİYONU (GROQ AI Destekli)
   =========================== */
function setupSearch() {
    const input = document.getElementById("searchInput");
    if (!input) return;

    const searchRow = document.getElementById("searchRow");

    const btnSearch = document.getElementById("btnSearch");
    if (btnSearch) {
        btnSearch.addEventListener("click", () => {
            input.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true }));
        });
    }

    input.addEventListener("keydown", async (evt) => {
        if (evt.key !== "Enter") return;
        evt.preventDefault();

        const query = input.value.trim();

        if (!query) {
            clearAISuggestions();
            const title = document.getElementById("sectionTitle");
            const subtitle = document.getElementById("sectionSubtitle");
            if (title) title.textContent = "Popular in your campus";
            if (subtitle) subtitle.textContent = "Highlighted by department.";
            await fetchBooks();
            return;
        }

        const title = document.getElementById("sectionTitle");
        const subtitle = document.getElementById("sectionSubtitle");
        if (title) title.textContent = "Discover Books for You";
        if (subtitle) subtitle.textContent = `AI-powered results for "${query}"`;

        if (searchRow) searchRow.style.opacity = "0.6";

        try {
            // Tüm kitapları Groq'a gönder
            const booksContext = books.map(b => ({
                bookId: b.bookId,
                title: b.title || "",
                author: b.author || "",
                category: b.category || "",
                description: b.description || ""
            }));

            const response = await fetch(`${window.API_BASE_URL}/ai/search-query`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "include",
                body: JSON.stringify({ query, books: booksContext })
            });

            if (!response.ok) {
                await searchBooks(query);
                return;
            }

            const data = await response.json();

            if (!data.success || !data.data) {
                await searchBooks(query);
                return;
            }

            const aiResult = data.data;

            // 1. Eşleşen kitaplar — kategorilere render et
            const matchedBooks = books.filter(book =>
                aiResult.matchedBookIds?.map(id => String(id)).includes(String(book.bookId))
            );
            renderBooks(matchedBooks.length > 0 ? matchedBooks : null);

            // 2. AI önerileri — eşleşmeyen ama ilgili kitaplar
            const recommendedBooks = books.filter(book =>
                aiResult.recommendedBookIds?.map(id => String(id)).includes(String(book.bookId)) &&
                !aiResult.matchedBookIds?.map(id => String(id)).includes(String(book.bookId))
            );

            if (recommendedBooks.length > 0) {
                renderAISuggestions(query, aiResult, recommendedBooks);
            }

        } finally {
            if (searchRow) searchRow.style.opacity = "1";
        }
    });
}

async function searchBooks(query) {
    try {
        const response = await fetch(
            `${window.API_BASE_URL}/books/search?query=${encodeURIComponent(query)}`,
            { credentials: 'include' }
        );
        const data = await response.json();
        if (data.success && data.data) {
            books = data.data;
            renderBooks();
        }
    } catch (error) {
        console.error('Search error:', error);
    }
}

/* ===========================
   BUTONLAR (Browse & Swap)
   =========================== */
function setupButtons() {
    const btnBrowse = document.getElementById("btnBrowse");
    const btnSwap = document.getElementById("btnSwap");

    if (btnBrowse) {
        btnBrowse.addEventListener("click", () => {
            document.querySelector('.section-header')?.scrollIntoView({ behavior: 'smooth' });
        });
    }

  // ESKİ
if (btnSwap) {
    btnSwap.addEventListener("click", () => {
        const addBookBtn = document.getElementById("btnAddBook");
        if (addBookBtn) addBookBtn.click();
    });
}
}

/* ===========================
   ADD BOOK MODAL
   =========================== */
function setupAddBookModal() {
    const overlay = document.querySelector(".add-book-overlay");
    const openBtn = document.getElementById("btnAddBook");
    const closeBtn = document.getElementById("btnCloseAddBook");
    const cancelBtn = document.getElementById("btnCancelAddBook");
    const form = document.getElementById("addBookForm");
    const imageFileInput = document.getElementById("imageFile");
    const imagePreview = document.getElementById("imagePreview");

    if (!overlay || !form) return;

    const open = () => {
        if (!currentUser) {
            alert('Please login first to add a book');
            window.location.href = '/login.html';
            return;
        }
        overlay.style.display = 'flex';
    };

    const close = () => {
        overlay.style.display = 'none';
        form.reset();
        if (imagePreview) {
            imagePreview.style.backgroundImage = '';
            imagePreview.innerHTML = `
                <div class="add-book-image-placeholder">
                    <span class="add-book-image-icon">📚</span>
                    <span class="add-book-image-text">Click to upload cover</span>
                </div>`;
        }
    };

    if (openBtn) openBtn.addEventListener("click", open);
    if (closeBtn) closeBtn.addEventListener("click", close);
    if (cancelBtn) cancelBtn.addEventListener("click", close);
    overlay.addEventListener("click", e => { if (e.target === overlay) close(); });

    if (imageFileInput && imagePreview) {
        imageFileInput.addEventListener("change", () => {
            const file = imageFileInput.files[0];
            if (!file) {
                imagePreview.style.backgroundImage = "";
                imagePreview.innerHTML = `
                    <div class="add-book-image-placeholder">
                        <span class="add-book-image-icon">📚</span>
                        <span class="add-book-image-text">Click to upload cover</span>
                    </div>`;
                return;
            }
            const reader = new FileReader();
            reader.onload = e => {
                imagePreview.style.backgroundImage = `url('${e.target.result}')`;
                imagePreview.innerHTML = '';
            };
            reader.readAsDataURL(file);
        });
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        await handleAddBook(close);
    });
}

async function handleAddBook(closeModal) {
    const title = document.getElementById("title").value.trim();
    const author = document.getElementById("author").value.trim();
    const category = document.getElementById("category").value;
    const description = document.getElementById("description").value.trim();
    const canBorrow = document.getElementById("availBorrow").checked;
    const canSwap = document.getElementById("availSwap").checked;
    const imageFile = document.getElementById("imageFile").files[0];

    if (!title || !author || !category) { alert("Please fill all required fields"); return; }
    if (!canBorrow && !canSwap) { alert("Please select at least one availability option"); return; }

    try {
        let coverImageBase64 = null;
        if (imageFile) coverImageBase64 = await fileToBase64(imageFile);

        const response = await fetch(`${window.API_BASE_URL}/books`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ title, author, category, description, coverImageBase64, canBeBorrowed: canBorrow, canBeSwapped: canSwap })
        });

        const data = await response.json();
        if (data.success) {
            alert('Book added successfully!');
            closeModal();
            await fetchBooks();
        } else {
            alert(data.message || 'Failed to add book');
        }
    } catch (error) {
        console.error('Error adding book:', error);
        alert('Error adding book. Please try again.');
    }
}

function fileToBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

/* ===========================
   KİTAP DETAY MODAL
   =========================== */
async function openBookDetail(book) {
    try {
        const response = await fetch(`${window.API_BASE_URL}/books/${book.bookId}`, { credentials: 'include' });
        const data = await response.json();

        if (!data.success || !data.data) { alert('Book details could not be loaded'); return; }

        const bookDetail = data.data;
        const overlay = document.getElementById("bookDetailOverlay");
        const detailCover = document.getElementById("bookDetailCover");
        const detailTitle = document.getElementById("bookDetailTitle");
        const detailAuthor = document.getElementById("bookDetailAuthor");
        const detailTag = document.getElementById("bookDetailTag");
        const detailOwner = document.getElementById("bookDetailOwner");
        const detailCategory = document.getElementById("bookDetailCategory");
        const detailStatus = document.getElementById("bookDetailStatus");
        const detailDescription = document.getElementById("bookDetailDescription");

        if (!overlay || !detailCover || !detailTitle || !detailAuthor ||
            !detailTag || !detailOwner || !detailCategory || !detailStatus || !detailDescription) {
            console.error("Book detail modal elements missing");
            return;
        }

        const coverImage = bookDetail.coverImageUrl || 'https://via.placeholder.com/300x400?text=No+Cover';
        detailCover.style.backgroundImage = `url('${coverImage}')`;
        detailTitle.textContent = bookDetail.title;
        detailAuthor.textContent = `by ${bookDetail.author}`;
        detailTag.textContent = bookDetail.category;
        detailOwner.textContent = bookDetail.ownerName;
        detailCategory.textContent = bookDetail.category;
        detailDescription.textContent = bookDetail.description || "No description available.";
        detailStatus.textContent = statusText[bookDetail.status];
        detailStatus.className = `status ${getStatusClass(bookDetail.status)}`;

        const closeOverlay = () => { overlay.style.display = 'none'; };

        const btnRequestBorrow = document.getElementById("btnRequestBorrow");
        const btnRequestSwap = document.getElementById("btnRequestSwap");

        if (!btnRequestBorrow || !btnRequestSwap) { console.error("Borrow or swap button not found"); return; }

        btnRequestBorrow.disabled = (bookDetail.status !== "Available" || !bookDetail.canBeBorrowed);
        btnRequestSwap.disabled = (bookDetail.status !== "Available" || !bookDetail.canBeSwapped);

        btnRequestBorrow.onclick = async () => {
            const message = document.getElementById("requestMessage").value.trim();
            try {
                const res = await fetch(`${window.API_BASE_URL}/requests`, {
                    method: "POST", headers: { "Content-Type": "application/json" }, credentials: "include",
                    body: JSON.stringify({ bookId: bookDetail.bookId, requestType: "Borrow", message })
                });
                if (!res.ok) throw new Error(`HTTP error: ${res.status}`);
                const result = await res.json();
                if (result.success) { alert("Borrow request sent successfully"); document.getElementById("requestMessage").value = ""; closeOverlay(); await fetchBooks(); }
                else alert(result.message || "Failed to send borrow request");
            } catch (error) { console.error("Borrow request error:", error); alert("An error occurred"); }
        };

        btnRequestSwap.onclick = async () => {
            const message = document.getElementById("requestMessage").value.trim();
            try {
                const res = await fetch(`${window.API_BASE_URL}/requests`, {
                    method: "POST", headers: { "Content-Type": "application/json" }, credentials: "include",
                    body: JSON.stringify({ bookId: bookDetail.bookId, requestType: "Swap", message })
                });
                if (!res.ok) throw new Error(`HTTP error: ${res.status}`);
                const result = await res.json();
                if (result.success) { alert("Swap request sent successfully"); document.getElementById("requestMessage").value = ""; closeOverlay(); await fetchBooks(); }
                else alert(result.message || "Failed to send swap request");
            } catch (error) { console.error("Swap request error:", error); alert("An error occurred"); }
        };

        document.getElementById("bookDetailClose").onclick = closeOverlay;
        overlay.onclick = (e) => { if (e.target === overlay) closeOverlay(); };
        overlay.style.display = 'flex';

    } catch (error) {
        console.error('Error opening book detail:', error);
        alert('Error loading book details');
    }
}

/* ===========================
   NAV LİNKLERİ
   =========================== */
function setupNavLinks() {
    const profileBtns = document.querySelectorAll(".nav-icon-btn[title='Profile']");
    profileBtns.forEach(btn => {
        btn.addEventListener("click", () => { window.location.href = "/profile.html"; });
    });
}

/* ===========================
   HTML ESCAPE
   =========================== */
function escapeHtml(text) {
    if (!text && text !== 0) return "";
    return String(text)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

/* ===========================
   DEBUG
   =========================== */
window._campus = {
    get books() { return books; },
    get currentUser() { return currentUser; },
    renderBooks,
    openBookDetail
};