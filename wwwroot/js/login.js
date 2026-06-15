// =======================
//  ARKA PLAN - UÇAN KİTAPLAR
// =======================
document.addEventListener("DOMContentLoaded", () => {

    // auth.js'te zaten tanımlı, const yerine direkt window üzerinden okuyoruz
    const loginApiUrl = window.API_BASE_URL;

    const canvas = document.getElementById("bgCanvas");
    if (!canvas) return;
    const ctx = canvas.getContext("2d");

    let books = [];
    const BOOK_COUNT = 18;

    function resizeCanvas() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
    }
    resizeCanvas();
    window.addEventListener("resize", () => {
        resizeCanvas();
        initBooks();
    });

    class Book {
        constructor() { this.reset(true); }
        reset(initial = false) {
            this.x = Math.random() * canvas.width;
            this.y = initial ? Math.random() * canvas.height : -80;
            this.width = Math.random() * 40 + 70;
            this.height = this.width * 0.65;
            this.vx = (Math.random() - 0.5) * 0.1;
            this.vy = Math.random() * 0.4 + 0.3;
            this.rotation = (Math.random() - 0.5) * 0.2;
            this.rotationSpeed = (Math.random() - 0.5) * 0.001;
            const palettes = [
                ["#2f80ed", "#1b4f9c"],
                ["#eb5757", "#9b2226"],
                ["#f2c94c", "#b8860b"],
                ["#27ae60", "#145a32"],
                ["#9b51e0", "#5b2d8f"]
            ];
            const p = palettes[Math.floor(Math.random() * palettes.length)];
            this.coverColor = p[0];
            this.spineColor = p[1];
            this.pageColor = "#fdf7e9";
        }
        update() {
            this.x += this.vx;
            this.y += this.vy;
            this.rotation += this.rotationSpeed;
            if (this.y - this.height > canvas.height + 100) this.reset(false);
        }
        draw() {
            ctx.save();
            ctx.translate(this.x, this.y);
            ctx.rotate(this.rotation);
            const w = this.width, h = this.height, thickness = 10;
            // Arka kapak
            ctx.fillStyle = this.spineColor;
            ctx.fillRect(-w / 2 + thickness, -h / 2, w, h);
            // Ön kapak
            ctx.fillStyle = this.coverColor;
            ctx.fillRect(-w / 2, -h / 2, w, h);
            // Sayfa
            ctx.fillStyle = this.pageColor;
            ctx.fillRect(-w / 2 + 12, -h / 2 + 8, w - 24, h - 16);
            ctx.restore();
        }
    }

    function initBooks() {
        books = [];
        for (let i = 0; i < BOOK_COUNT; i++) books.push(new Book());
    }

    function animateBackground() {
        const grad = ctx.createLinearGradient(0, 0, canvas.width, canvas.height);
        grad.addColorStop(0, "#050814");
        grad.addColorStop(0.5, "#0b1838");
        grad.addColorStop(1, "#050814");
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        books.forEach(b => { b.update(); b.draw(); });
        requestAnimationFrame(animateBackground);
    }

    initBooks();
    animateBackground();

    // =======================
    //  TAB GEÇİŞİ
    // =======================
    const tabButtons = document.querySelectorAll(".tab-btn");
    const forms = document.querySelectorAll(".form");
    const title = document.getElementById("formTitle");

    tabButtons.forEach(btn => {
        btn.addEventListener("click", () => {
            tabButtons.forEach(b => b.classList.remove("active"));
            btn.classList.add("active");
            forms.forEach(f => f.classList.remove("active"));
            document.getElementById(btn.dataset.target).classList.add("active");
            title.textContent = btn.dataset.target === "signInForm" ? "User Registration" : "User Login";
        });
    });

    // =======================
    //  REGISTER FORM
    // =======================
    const signInForm = document.getElementById("signInForm");
    const registerError = document.getElementById("registerError");
    if (signInForm) {
        signInForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            const fullName = document.getElementById("registerFullName").value.trim();
            const email = document.getElementById("registerEmail").value.trim();
            const username = document.getElementById("registerUsername").value.trim();
            const password = document.getElementById("registerPassword").value;
            const confirmPassword = document.getElementById("registerConfirmPassword").value;

            if (registerError) { registerError.style.display = "none"; registerError.textContent = ""; }

            if (!fullName || !email || !username || !password || !confirmPassword) { return showError(registerError, "All fields are required"); }
            if (password !== confirmPassword) { return showError(registerError, "Passwords do not match"); }
            if (password.length < 6) { return showError(registerError, "Password must be at least 6 characters"); }
            if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { return showError(registerError, "Please enter a valid email address"); }

            try {
                const response = await fetch(`${loginApiUrl}/register`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    credentials: "include",
                    body: JSON.stringify({ fullName, email, username, password, confirmPassword })
                });
                const data = await response.json();
                if (data.success) {
                    alert("Registration successful! Please login.");
                    const loginTab = document.querySelector('[data-target="loginForm"]');
                    if (loginTab) loginTab.click();
                    signInForm.reset();
                } else showError(registerError, data.message || "Registration failed");
            } catch (err) {
                console.error("Register error:", err);
                showError(registerError, "Connection error. Please try again.");
            }
        });
    }

    // =======================
    //  LOGIN FORM
    // =======================
    const loginForm = document.getElementById("loginForm");
    const loginError = document.getElementById("loginError");
    if (loginForm) {
        loginForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            const username = document.getElementById("loginUsername").value.trim();
            const password = document.getElementById("loginPassword").value;
            if (loginError) { loginError.style.display = "none"; loginError.textContent = ""; }
            if (!username || !password) return showError(loginError, "Username and password are required");

            try {
                const response = await fetch(`${loginApiUrl}/login`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    credentials: "include",
                    body: JSON.stringify({ username, password, rememberMe: false })
                });
                if (!response.ok) {
                    throw new Error(`HTTP error: ${response.status}`);
                }
                const data = await response.json();
                if (data.success) {
                    alert("Login successful!");
                    window.location.href = "/index.html";
                } else showError(loginError, data.message || "Login failed");
            } catch (err) {
                console.error("Login error:", err);
                showError(loginError, "Connection error. Please try again.");
            }
        });
    }

    // =======================
    //  HELPER: HATA GÖSTER
    // =======================
    function showError(el, msg) {
        if (el) { el.textContent = msg; el.style.display = "block"; }
    }
});