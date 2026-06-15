// ========== API BASE URL ==========
// ⚠️ SADECE BURADA TANIMLI - Diğer dosyalar window.API_BASE_URL kullanacak
window.API_BASE_URL = 'http://localhost:5248/api';


// ==================== SAYFA YÜKLENDİĞİNDE OTURUM KONTROLÜ ====================
document.addEventListener('DOMContentLoaded', async () => {
    await initAuth();
});

// ==================== AUTH BAŞLATMA ====================
async function initAuth() {
    const user = await checkAuth();

    if (user) {
        handleAuthenticatedUser(user);
    } else {
        handleUnauthenticatedUser();
    }
    // Bildirimleri başlat
    if (window.initNotifications) {
        window.initNotifications();
    }
}

// ==================== CHECK AUTH (Oturum Kontrolü) ====================
async function checkAuth() {
    try {
        const response = await fetch(`${window.API_BASE_URL}/checkauth`, {
            method: 'GET',
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(`HTTP error: ${response.status}`);
        }

        const data = await response.json();

        if (data.success && data.data) {
            console.log('✅ User authenticated:', data.data);
            return data.data;
        }

        console.log('❌ User not authenticated');
        return null;
    } catch (error) {
        console.error('Auth check error:', error);
        return null;
    }
}

// ==================== GİRİŞ YAPMIŞ KULLANICI İÇİN UI GÜNCELLEMELERI ====================
function handleAuthenticatedUser(userData) {
    console.log('Setting up UI for authenticated user:', userData);

    // 1. Sign in butonunu Logout olarak değiştir
    const btnAuth = document.getElementById('btnAuth');
    if (btnAuth) {
        btnAuth.textContent = 'Logout';
        btnAuth.title = 'Logout';
        btnAuth.classList.remove('nav-cta');
        btnAuth.classList.add('nav-cta-logout');
        btnAuth.onclick = logout;
    }

    // 2. Kullanıcı bilgi kutusunu göster
    const userInfoSection = document.getElementById('userInfoSection');
    const welcomeUser = document.getElementById('welcomeUser');
    const userEmail = document.getElementById('userEmail');

    if (userInfoSection && welcomeUser && userEmail) {
        welcomeUser.textContent = `Welcome, ${userData.fullName}!`;
        userEmail.textContent = userData.email;
        userInfoSection.style.display = 'block';
    }

    // 3. Logout butonuna event ekle
    const btnLogout = document.getElementById('btnLogout');
    if (btnLogout) {
        btnLogout.onclick = logout;
    }

    // 4. Profile butonunu aktif et
    const btnProfile = document.getElementById('btnProfile');
    if (btnProfile) {
        btnProfile.onclick = () => {
            window.location.href = '/profile.html';
        };
        btnProfile.style.cursor = 'pointer';
        btnProfile.style.opacity = '1';
    }

    // 5. Add Book butonunu aktif et
    const btnAddBook = document.getElementById('btnAddBook');
    if (btnAddBook) {
        btnAddBook.style.opacity = '1';
        btnAddBook.style.cursor = 'pointer';
    }

    // 6. localStorage'a kaydet
    localStorage.setItem('user', JSON.stringify(userData));
    localStorage.setItem('isAuthenticated', 'true');
}

// ==================== GİRİŞ YAPMAMIŞ KULLANICI İÇİN UI GÜNCELLEMELERI ====================
function handleUnauthenticatedUser() {
    console.log('Setting up UI for unauthenticated user');

    // 1. Sign in butonu göster
    const btnAuth = document.getElementById('btnAuth');
    if (btnAuth) {
        btnAuth.textContent = 'Sign in';
        btnAuth.title = 'Sign in';
        btnAuth.classList.add('nav-cta');
        btnAuth.classList.remove('nav-cta-logout');
        btnAuth.onclick = () => {
            window.location.href = '/login.html';
        };
    }

    // 2. Kullanıcı bilgi kutusunu gizle
    const userInfoSection = document.getElementById('userInfoSection');
    if (userInfoSection) {
        userInfoSection.style.display = 'none';
    }

    // 3. Profile butonunu devre dışı bırak
    const btnProfile = document.getElementById('btnProfile');
    if (btnProfile) {
        btnProfile.onclick = () => {
            alert('⚠️ Please login first!');
            window.location.href = '/login.html';
        };
        btnProfile.style.cursor = 'not-allowed';
        btnProfile.style.opacity = '0.5';
    }

    // 4. Add Book butonunu devre dışı bırak
    const btnAddBook = document.getElementById('btnAddBook');
    if (btnAddBook) {
        btnAddBook.onclick = () => {
            alert('⚠️ Please login to add a book!');
            window.location.href = '/login.html';
        };
        btnAddBook.style.opacity = '0.7';
        btnAddBook.style.cursor = 'pointer';
    }

    // 5. localStorage'ı temizle
    localStorage.removeItem('user');
    localStorage.removeItem('isAuthenticated');
}

// ==================== LOGOUT ====================
async function logout() {
    if (!confirm('Are you sure you want to logout?')) {
        return;
    }

    try {
        const response = await fetch(`${window.API_BASE_URL}/logout`, {
            method: 'POST',
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(`HTTP error: ${response.status}`);
        }

        const data = await response.json();

        if (data.success) {
            localStorage.removeItem('user');
            localStorage.removeItem('isAuthenticated');

            alert('✅ Logout successful!');
            window.location.href = '/login.html';
        } else {
            alert('❌ Logout failed: ' + data.message);
        }
    } catch (error) {
        console.error('Logout error:', error);
        alert('❌ An error occurred during logout!');
    }
}

// ==================== YARDIMCI FONKSİYONLAR ====================
function getCurrentUser() {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
}

async function requireAuth() {
    const user = await checkAuth();

    if (!user) {
        alert('⚠️ Please login to access this page!');
        window.location.href = '/login.html';
        return false;
    }

    return true;
}

function isAuthenticated() {
    return localStorage.getItem('isAuthenticated') === 'true';
}

// ==================== GLOBAL ERİŞİM ====================
window.checkAuth = checkAuth;
window.logout = logout;
window.getCurrentUser = getCurrentUser;
window.requireAuth = requireAuth;
window.isAuthenticated = isAuthenticated;