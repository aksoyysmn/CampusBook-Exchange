// ==================== GLOBAL DEĞİŞKENLER ====================
let currentUser = null;
let allBooks = []; // Kapak resmi bulmak için tüm kitaplar

function getCoverByBookId(bookId) {
    const found = allBooks.find(b => String(b.bookId) === String(bookId));
    return found?.coverImageUrl || null;
}

// ==================== SAYFA YÜKLENİNCE ====================
document.addEventListener("DOMContentLoaded", async () => {
    const authUser = await window.checkAuth();
    if (!authUser) {
        alert('Please login first');
        window.location.href = '/login.html';
        return;
    }
    currentUser = authUser;
    await loadProfileData();
    setupTabs();
    setupEditForm();
    toggleEditMode(false);
});

// ==================== PROFİL VERİLERİNİ YÜKle ====================
async function loadProfileData() {
    try {
        // Önce tüm kitapları çek - kapak resmi eşleştirmesi için
        try {
            const booksRes = await fetch(`${window.API_BASE_URL}/books`, { credentials: 'include' });
            const booksData = await booksRes.json();
            if (booksData.success && booksData.data) allBooks = booksData.data;
        } catch (e) {
            console.warn('Could not load books for cover matching:', e);
        }

        const profileResponse = await fetch(`${window.API_BASE_URL}/profile`, { credentials: 'include' });
        if (!profileResponse.ok) throw new Error('Failed to load profile: ' + profileResponse.status);

        const profileResult = await profileResponse.json();
        if (!(profileResult.success && profileResult.data)) throw new Error('Profile data not found');

        const profile = profileResult.data;

        updateElement('profileName', profile.fullName);
        updateElement('profileEmail', profile.email);
        updateElement('profileEmailDetail', profile.email);
        updateElement('profileUsername', '@' + profile.username);

        currentUser = {
            userId: profile.userId,
            username: profile.username,
            email: profile.email,
            fullName: profile.fullName
        };
        localStorage.setItem('user', JSON.stringify(currentUser));

        const myBooks = profile.myBooks || [];
        const sentRequests = profile.sentRequests || [];
        const receivedRequests = profile.receivedRequests || [];

        updateElement('statsMyBooks', myBooks.length);
        updateElement('statsSentRequests', sentRequests.length);
        updateElement('statsReceivedRequests', receivedRequests.length);

        renderMyBooks(myBooks);
        renderSentRequests(sentRequests);
        renderReceivedRequests(receivedRequests);

    } catch (error) {
        console.error('Error loading profile:', error);
        if (currentUser) {
            updateElement('profileName', currentUser.fullName);
            updateElement('profileEmail', currentUser.email);
            updateElement('profileEmailDetail', currentUser.email);
            updateElement('profileUsername', '@' + currentUser.username);
        }
        updateElement('statsMyBooks', 0);
        updateElement('statsSentRequests', 0);
        updateElement('statsReceivedRequests', 0);
        renderMyBooks([]);
        renderSentRequests([]);
        renderReceivedRequests([]);
    }
}

function updateElement(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value;
    else console.warn('Element not found: ' + id);
}

function getStatusBadge(status) {
    if (status === 'Accepted') return 'badge-success';
    if (status === 'Pending') return 'badge-warning';
    if (status === 'Rejected') return 'badge-danger';
    return 'badge-secondary';
}

// ==================== MY BOOKS RENDER ====================
function renderMyBooks(books) {
    const container = document.getElementById('myBooksContainer');
    if (!container) return;
    if (!books || books.length === 0) {
        container.innerHTML = '<div class="empty-state"><h3>You haven\'t added any books yet</h3><p>Start sharing books with your campus community!</p></div>';
        return;
    }
    container.innerHTML = books.map(book => `
        <div class="book-item">
            <img src="${book.coverImageUrl || 'https://via.placeholder.com/60x85?text=No+Cover'}"
                 alt="${book.title || 'Book'}" class="book-item-cover"
                 onerror="this.src='https://via.placeholder.com/60x85?text=No+Cover'" />
            <div class="book-item-info">
                <h4>${book.title || 'Untitled Book'}</h4>
                <p>${book.author || 'Unknown Author'}</p>
            </div>
            <div class="book-item-actions">
                <button class="btn-icon delete-book-btn" onclick="deleteBook('${book.bookId}')" title="Delete">🗑️</button>
            </div>
        </div>`).join('');
}

// ==================== SENT REQUESTS RENDER ====================
function renderSentRequests(requests) {
    const container = document.getElementById('sentRequestsContainer');
    if (!container) return;
    if (!requests || requests.length === 0) {
        container.innerHTML = '<div class="empty-state"><h3>You haven\'t sent any requests yet</h3></div>';
        return;
    }
    container.innerHTML = requests.map(r => {
        const cover = r.coverImageUrl || getCoverByBookId(r.bookId) || 'https://via.placeholder.com/70x100?text=No+Cover';
        return `
        <div class="request-item">
            <img src="${cover}" alt="${r.bookTitle || 'Book'}" class="request-item-cover"
                 onerror="this.src='https://via.placeholder.com/70x100?text=No+Cover'" />
            <div class="request-item-info">
                <h4>${r.bookTitle || 'Unknown Book'}</h4>
                <p class="text-muted">by ${r.bookAuthor || '-'}</p>
                <p class="text-muted">Owner: ${r.ownerName || '-'}</p>
                <p class="text-muted">Type: ${r.requestType || '-'}</p>
                ${r.message ? '<p class="text-muted">Message: ' + r.message + '</p>' : ''}
            </div>
            <div class="request-item-status">
                <span class="badge ${getStatusBadge(r.status)}">${r.status || '-'}</span>
                ${r.status === 'Pending' ? '<button onclick="cancelRequest(' + r.requestId + ')" class="btn-secondary small">🚫 Cancel</button>' : ''}
            </div>
        </div>`;
    }).join('');
}

// ==================== RECEIVED REQUESTS RENDER ====================
function renderReceivedRequests(requests) {
    const container = document.getElementById('receivedRequestsContainer');
    if (!container) return;
    if (!requests || requests.length === 0) {
        container.innerHTML = '<div class="empty-state"><h3>No requests received yet</h3></div>';
        return;
    }
    container.innerHTML = requests.map(r => {
        const cover = r.coverImageUrl || getCoverByBookId(r.bookId) || 'https://via.placeholder.com/70x100?text=No+Cover';
        return `
        <div class="request-item">
            <img src="${cover}" alt="${r.bookTitle || 'Book'}" class="request-item-cover"
                 onerror="this.src='https://via.placeholder.com/70x100?text=No+Cover'" />
            <div class="request-item-info">
                <h4>${r.bookTitle || 'Unknown Book'}</h4>
                <p class="text-muted">by ${r.bookAuthor || '-'}</p>
                <p class="text-muted">Requester: ${r.requesterName || '-'}</p>
                <p class="text-muted">Type: ${r.requestType || '-'}</p>
                ${r.message ? '<p class="text-muted">Message: ' + r.message + '</p>' : ''}
            </div>
            <div class="request-item-status">
                <span class="badge ${getStatusBadge(r.status)}">${r.status || '-'}</span>
                ${r.status === 'Pending' ? `
                    <div style="display:flex;flex-direction:column;gap:6px;">
                        <button onclick="respondToRequest(${r.requestId},'accept')" class="btn-primary small">✅ Accept</button>
                        <button onclick="respondToRequest(${r.requestId},'reject')" class="btn-secondary small">❌ Reject</button>
                    </div>` : ''}
            </div>
        </div>`;
    }).join('');
}

// ==================== RESPOND TO REQUEST ====================
async function respondToRequest(requestId, action) {
    if (!confirm('Are you sure you want to ' + action + ' this request?')) return;
    try {
        const response = await fetch(`${window.API_BASE_URL}/requests/${requestId}/respond`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ action })
        });
        const result = await response.json();
        if (result.success) { alert('✅ ' + result.message); await loadProfileData(); }
        else alert('❌ ' + (result.message || 'Failed to respond'));
    } catch (error) {
        console.error('Respond error:', error);
        alert('❌ An error occurred');
    }
}

// ==================== CANCEL REQUEST ====================
async function cancelRequest(requestId) {
    if (!confirm('Are you sure you want to cancel this request?')) return;
    try {
        const response = await fetch(`${window.API_BASE_URL}/requests/${requestId}/cancel`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include'
        });
        const result = await response.json();
        if (result.success) { alert('✅ Request cancelled successfully'); await loadProfileData(); }
        else alert('❌ ' + (result.message || 'Failed to cancel request'));
    } catch (error) {
        console.error('Cancel error:', error);
        alert('❌ An error occurred');
    }
}

// ==================== TABS ====================
function setupTabs() {
    const buttons = document.querySelectorAll('.tab-button');
    const tabs = document.querySelectorAll('.tab-content');
    if (!buttons.length || !tabs.length) return;
    buttons.forEach(btn => {
        btn.addEventListener('click', () => {
            const tabName = btn.dataset.tab;
            buttons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            tabs.forEach(tab => {
                tab.id === 'tab-' + tabName ? tab.classList.add('active') : tab.classList.remove('active');
            });
        });
    });
}

// ==================== EDIT FORM ====================
function setupEditForm() {
    [document.getElementById('btnEditProfile'), document.getElementById('btnEditProfileTop')]
        .forEach(btn => { if (btn) btn.addEventListener('click', () => toggleEditMode(true)); });
    const btnCancel = document.getElementById('btnCancelEdit');
    if (btnCancel) btnCancel.addEventListener('click', () => toggleEditMode(false));
    const editForm = document.getElementById('editForm');
    if (editForm) editForm.addEventListener('submit', handleEditSubmit);
    const btnLogout = document.getElementById('btnLogout');
    if (btnLogout) btnLogout.onclick = handleLogout;

    // Şifre değiştirme paneli
    const btnChangePassword = document.getElementById('btnChangePassword');
    if (btnChangePassword) btnChangePassword.addEventListener('click', () => toggleChangePasswordPanel(true));

    const btnCancelChangePassword = document.getElementById('btnCancelChangePassword');
    if (btnCancelChangePassword) btnCancelChangePassword.addEventListener('click', () => toggleChangePasswordPanel(false));

    const changePasswordForm = document.getElementById('changePasswordForm');
    if (changePasswordForm) changePasswordForm.addEventListener('submit', handleChangePassword);
}

// ==================== ŞİFRE PANELİ ====================
function togglePasswordPanel(show) {
    const viewPanel = document.getElementById('viewPanel');
    const editPanel = document.getElementById('editPanel');
    const changePasswordPanel = document.getElementById('changePasswordPanel');
    const passwordError = document.getElementById('passwordError');

    if (!changePasswordPanel) return;

    if (show) {
        if (viewPanel) viewPanel.style.display = 'none';
        if (editPanel) editPanel.style.display = 'none';
        changePasswordPanel.style.display = 'block';
        // Formu sıfırla
        document.getElementById('changePasswordForm')?.reset();
        if (passwordError) { passwordError.style.display = 'none'; passwordError.textContent = ''; }
    } else {
        changePasswordPanel.style.display = 'none';
        if (viewPanel) viewPanel.style.display = 'block';
    }
}

async function handleChangePassword(e) {
    e.preventDefault();

    const currentPassword = document.getElementById('currentPassword')?.value;
    const newPassword = document.getElementById('newPassword')?.value;
    const confirmNewPassword = document.getElementById('confirmNewPassword')?.value;
    const passwordError = document.getElementById('passwordError');

    // Validation
    if (newPassword !== confirmNewPassword) {
        if (passwordError) { passwordError.textContent = '❌ New passwords do not match'; passwordError.style.display = 'block'; }
        return;
    }

    if (newPassword.length < 6) {
        if (passwordError) { passwordError.textContent = '❌ Password must be at least 6 characters'; passwordError.style.display = 'block'; }
        return;
    }

    try {
        const response = await fetch(`${window.API_BASE_URL}/profile/change-password`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                CurrentPassword: currentPassword,
                NewPassword: newPassword,
                ConfirmPassword: confirmNewPassword
            })
        });

        const result = await response.json();

        if (result.success) {
            alert('✅ Password changed successfully!');
            togglePasswordPanel(false);
        } else {
            if (passwordError) { passwordError.textContent = '❌ ' + (result.message || 'Failed to change password'); passwordError.style.display = 'block'; }
        }
    } catch (error) {
        console.error('Change password error:', error);
        if (passwordError) { passwordError.textContent = '❌ An error occurred'; passwordError.style.display = 'block'; }
    }
}

function toggleEditMode(showEdit) {
    const viewPanel = document.getElementById('viewPanel');
    const editPanel = document.getElementById('editPanel');
    if (!viewPanel || !editPanel) return;
    const changePasswordPanel = document.getElementById('changePasswordPanel');

    if (showEdit) {
        viewPanel.style.display = 'none';
        editPanel.style.display = 'block';
        if (changePasswordPanel) changePasswordPanel.style.display = 'none';
        fillEditForm();
    } else {
        viewPanel.style.display = 'block';
        editPanel.style.display = 'none';
        if (changePasswordPanel) changePasswordPanel.style.display = 'none';
    }
}

function fillEditForm() {
    if (!currentUser) return;
    const fn = document.getElementById('fullName');
    const em = document.getElementById('email');
    const un = document.getElementById('username');
    if (fn) fn.value = currentUser.fullName;
    if (em) em.value = currentUser.email;
    if (un) { un.value = currentUser.username; un.disabled = true; }
}

async function handleEditSubmit(e) {
    e.preventDefault();
    const fn = document.getElementById('fullName');
    const em = document.getElementById('email');
    if (!fn || !em) { alert('Form elements not found'); return; }
    const fullName = fn.value.trim();
    const email = em.value.trim();
    if (!fullName || !email) { alert('Full name and email are required'); return; }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { alert('Please enter a valid email address'); return; }
    try {
        const response = await fetch(`${window.API_BASE_URL}/profile`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ fullName, email })
        });
        const result = await response.json();
        if (result.success) {
            alert('✅ Profile updated successfully!');
            if (result.data) {
                currentUser.fullName = result.data.fullName;
                currentUser.email = result.data.email;
                localStorage.setItem('user', JSON.stringify(currentUser));
            }
            await loadProfileData();
            toggleEditMode(false);
        } else {
            alert('❌ ' + (result.message || 'Failed to update profile'));
        }
    } catch (error) {
        console.error('Error updating profile:', error);
        alert('❌ An error occurred while updating profile');
    }
}

// ==================== LOGOUT ====================
async function handleLogout() {
    if (!confirm('Are you sure you want to logout?')) return;
    try {
        const response = await fetch(`${window.API_BASE_URL}/logout`, { method: 'POST', credentials: 'include' });
        const result = await response.json();
        if (result.success) {
            localStorage.removeItem('user');
            localStorage.removeItem('isAuthenticated');
            alert('✅ Logged out successfully');
            window.location.href = '/login.html';
        } else {
            alert('❌ Logout failed');
        }
    } catch (error) {
        console.error('Logout error:', error);
        alert('❌ Logout failed');
    }
}

// ==================== DELETE BOOK ====================
async function deleteBook(bookId) {
    if (!bookId) { alert('Book id not found'); return; }
    if (!confirm('Are you sure you want to delete this book?')) return;
    try {
        const response = await fetch(`${window.API_BASE_URL}/books/${bookId}`, { method: 'DELETE', credentials: 'include' });
        const result = await response.json();
        if (result.success) { alert('✅ Book deleted successfully'); await loadProfileData(); }
        else alert('❌ ' + (result.message || 'Failed to delete book'));
    } catch (error) {
        console.error('Delete error:', error);
        alert('❌ An error occurred while deleting the book');
    }
}

// ==================== CHANGE PASSWORD PANEL ====================
function toggleChangePasswordPanel(show) {
    const viewPanel = document.getElementById('viewPanel');
    const editPanel = document.getElementById('editPanel');
    const changePasswordPanel = document.getElementById('changePasswordPanel');
    const errorEl = document.getElementById('changePasswordError');

    if (show) {
        if (viewPanel) viewPanel.style.display = 'none';
        if (editPanel) editPanel.style.display = 'none';
        if (changePasswordPanel) changePasswordPanel.style.display = 'block';
        if (errorEl) errorEl.style.display = 'none';
        // Formu sıfırla
        const form = document.getElementById('changePasswordForm');
        if (form) form.reset();
    } else {
        if (viewPanel) viewPanel.style.display = 'block';
        if (changePasswordPanel) changePasswordPanel.style.display = 'none';
    }
}

async function handleChangePassword(e) {
    e.preventDefault();

    const currentPassword = document.getElementById('currentPassword').value;
    const newPassword = document.getElementById('newPassword').value;
    const confirmNewPassword = document.getElementById('confirmNewPassword').value;
    const errorEl = document.getElementById('changePasswordError');

    // Frontend validasyon
    if (newPassword.length < 6) {
        errorEl.textContent = 'New password must be at least 6 characters';
        errorEl.style.display = 'block';
        return;
    }

    if (newPassword !== confirmNewPassword) {
        errorEl.textContent = 'New passwords do not match';
        errorEl.style.display = 'block';
        return;
    }

    errorEl.style.display = 'none';

    try {
        const response = await fetch(`${window.API_BASE_URL}/profile/change-password`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                CurrentPassword: currentPassword,
                NewPassword: newPassword,
                ConfirmPassword: confirmNewPassword
            })
        });

        const result = await response.json();

        if (result.success) {
            alert('✅ Password changed successfully!');
            toggleChangePasswordPanel(false);
        } else {
            errorEl.textContent = result.message || 'Failed to change password';
            errorEl.style.display = 'block';
        }
    } catch (error) {
        console.error('Change password error:', error);
        errorEl.textContent = 'An error occurred. Please try again.';
        errorEl.style.display = 'block';
    }
}