document.addEventListener('DOMContentLoaded', () => {
    // ================= GLOBAL STATE =================
    let currentUser = null;
    let isDbConnected = false;
    let currentOptimizedImageBase64 = null;
    let originalImageFile = null;
    let compressionQuality = 0.82;

    // Desktop App UX: Disable standard browser context menu (allow in input/textarea)
    window.addEventListener('contextmenu', (e) => {
        const tag = e.target.tagName;
        if (tag === 'INPUT' || tag === 'TEXTAREA' || e.target.isContentEditable) {
            return;
        }
        e.preventDefault();
    });

    // Desktop App UX: Prevent accidental browser keys like Ctrl+S, Ctrl+P, F3
    window.addEventListener('keydown', (e) => {
        if ((e.ctrlKey && (e.key === 's' || e.key === 'p' || e.key === 'u')) || e.key === 'F3') {
            e.preventDefault();
        }
    });

    // ================= DOM ELEMENTS =================
    // Topbar & Navigation
    const dbStatusBadge = document.getElementById('db-status-badge');
    const dbStatusText = document.getElementById('db-status-text');
    const userHeaderInfo = document.getElementById('user-header-info');
    const headerUserName = document.getElementById('header-user-name');
    const headerUserRole = document.getElementById('header-user-role');
    const btnLogout = document.getElementById('btn-logout');

    const navBtnDash = document.getElementById('nav-btn-dash');
    const navBtnPic = document.getElementById('nav-btn-pic');
    const navBtnUsers = document.getElementById('nav-btn-users');

    // Views
    const formLogin = document.getElementById('form-login');
    const formDashboard = document.getElementById('form-dashboard');
    const formImageReg = document.getElementById('form-image-registration');

    // Login Form
    const loginForm = document.getElementById('login-form');
    const usernameInput = document.getElementById('username');
    const passwordInput = document.getElementById('password');
    const togglePasswordBtn = document.getElementById('toggle-password');
    const loginAlert = document.getElementById('login-alert');
    const btnLoginSubmit = document.getElementById('btn-login-submit');

    // Dashboard
    const dashUserName = document.getElementById('dash-user-name');
    const statTotalUsers = document.getElementById('stat-total-users');
    const statTotalImages = document.getElementById('stat-total-images');
    const statServerName = document.getElementById('stat-server-name');
    const statServerStatus = document.getElementById('stat-server-status');
    const btnHeroOpenPic = document.getElementById('btn-hero-open-pic');
    const btnGotoPicForm = document.getElementById('btn-goto-pic-form');
    const btnGotoUsers = document.getElementById('btn-goto-users');
    const btnShowDbDetails = document.getElementById('btn-show-db-details');

    // Image Registration Form
    const receiptImageForm = document.getElementById('receipt-image-form');
    const picSerialNoInput = document.getElementById('pic-serial-no');
    const picReceiptNoInput = document.getElementById('pic-receipt-no');
    const picReceiptDateInput = document.getElementById('pic-receipt-date');
    const picRegisteredByInput = document.getElementById('pic-registered-by');
    const liveRegistrationTime = document.getElementById('live-registration-time');
    const receiptFormAlert = document.getElementById('receipt-form-alert');
    const btnSaveReceipt = document.getElementById('btn-save-receipt');
    const btnResetReceiptForm = document.getElementById('btn-reset-receipt-form');
    const btnBackToDash = document.getElementById('btn-back-to-dash');

    // ================= LIVE COMPUTER CLOCK SYNC =================
    function updateLiveComputerClock() {
        if (liveRegistrationTime) {
            const now = new Date();
            let h = now.getHours();
            const m = String(now.getMinutes()).padStart(2, '0');
            const s = String(now.getSeconds()).padStart(2, '0');
            const ampm = (h >= 12) ? 'پ.ن' : 'ب.ن';
            const h12 = h % 12 || 12;
            const pad12 = String(h12).padStart(2, '0');
            liveRegistrationTime.innerHTML = `🕒 ${pad12}:${m}:${s} <small style="opacity:0.85">(${ampm})</small>`;
        }
    }
    setInterval(updateLiveComputerClock, 1000);
    updateLiveComputerClock();

    function formatClockTime(timeStr, fullDateTimeStr) {
        if (timeStr) {
            const parts = timeStr.split(':');
            if (parts.length >= 2) {
                let h = parseInt(parts[0], 10);
                const m = parts[1];
                const s = parts[2] ? parts[2].substring(0, 2) : '00';
                const ampm = (h >= 12) ? 'پ.ن' : 'ب.ن';
                const h12 = h % 12 || 12;
                const pad12 = (h12 < 10 ? '0' : '') + h12;
                return `<span class="time-tag" title="کاتی تۆمارکردن بەپێی کاتژمێری کۆمپیوتەر: ${timeStr}">🕒 ${pad12}:${m}:${s} <small style="color:var(--text-muted); font-size:0.75rem;">(${ampm})</small></span>`;
            }
            return `<span class="time-tag">🕒 ${timeStr}</span>`;
        }
        if (fullDateTimeStr) {
            const t = fullDateTimeStr.includes(' ') ? fullDateTimeStr.split(' ')[1] : fullDateTimeStr;
            return `<span class="time-tag">🕒 ${t}</span>`;
        }
        return '-';
    }

    // Image Dropzone & Compression
    const imageDropzone = document.getElementById('image-dropzone');
    const picFileInput = document.getElementById('pic-file-input');
    const btnBrowseImage = document.getElementById('btn-browse-image');
    const dropzoneEmptyState = document.getElementById('dropzone-empty-state');
    const dropzonePreviewState = document.getElementById('dropzone-preview-state');
    const previewImageElem = document.getElementById('preview-image-elem');
    const btnRemoveImage = document.getElementById('btn-remove-image');
    const btnEnlargePreview = document.getElementById('btn-enlarge-preview');

    const compressionMeter = document.getElementById('compression-meter');
    const meterOrigSize = document.getElementById('meter-orig-size');
    const meterOptSize = document.getElementById('meter-opt-size');
    const meterSavings = document.getElementById('meter-savings');
    const qualityControls = document.getElementById('quality-controls');
    const qualitySlider = document.getElementById('quality-slider');
    const qualityValText = document.getElementById('quality-val-text');

    // Records Table
    const inputSearchImages = document.getElementById('input-search-images');
    const btnRefreshImages = document.getElementById('btn-refresh-images');
    const imagesTableBody = document.getElementById('images-table-body');

    // Modals
    const modalUsers = document.getElementById('modal-users');
    const btnCloseUsersModal = document.getElementById('btn-close-users-modal');
    const addUserForm = document.getElementById('add-user-form');
    const addUserAlert = document.getElementById('add-user-alert');
    const usersTableBody = document.getElementById('users-table-body');

    const modalDbInfo = document.getElementById('modal-db-info');
    const btnCloseDbModal = document.getElementById('btn-close-db-modal');
    const btnRetestDb = document.getElementById('btn-retest-db');
    const infoServerVal = document.getElementById('info-server-val');

    const modalLightbox = document.getElementById('modal-lightbox');
    const btnCloseLightbox = document.getElementById('btn-close-lightbox');
    const btnLightboxPrint = document.getElementById('btn-lightbox-print');
    const lightboxImg = document.getElementById('lightbox-img');
    const lightboxTitle = document.getElementById('lightbox-title');
    const lightboxDownloadLink = document.getElementById('lightbox-download-link');
    const lightboxInfoBar = document.getElementById('lightbox-info-bar');
    let currentReceiptPrintData = null;

    // ================= 1. SQL SERVER HEALTH CHECK =================
    async function checkDbStatus() {
        try {
            const res = await fetch('/api/status');
            const data = await res.json();

            if (data.connected) {
                isDbConnected = true;
                dbStatusBadge.className = 'status-badge connected';
                dbStatusText.textContent = `پەیوەستە بە سێرڤەر`;
                if (statServerName) statServerName.textContent = 'چالاکە';
                if (statServerStatus) statServerStatus.textContent = 'پەیوەستە بە لۆکاڵ';
                if (statTotalUsers && data.totalUsers !== undefined) statTotalUsers.textContent = data.totalUsers;
                if (statTotalImages && data.totalImages !== undefined) statTotalImages.textContent = data.totalImages;
                if (infoServerVal) infoServerVal.textContent = `سێرڤەری لۆکاڵ (چالاکە)`;
            } else {
                isDbConnected = false;
                dbStatusBadge.className = 'status-badge disconnected';
                dbStatusText.textContent = 'پەیوەندی پچڕاوە بە سێرڤەر';
                if (statServerStatus) statServerStatus.textContent = 'پچڕاوە';
            }
        } catch (err) {
            isDbConnected = false;
            dbStatusBadge.className = 'status-badge disconnected';
            dbStatusText.textContent = 'هەڵە لە پەیوەندی بە سێرڤەر';
        }
    }

    // Live Clock
    setInterval(() => {
        const now = new Date();
        const timeStr = now.toLocaleTimeString('ku', { hour12: true });
        if (liveRegistrationTime) liveRegistrationTime.textContent = timeStr;
    }, 1000);

    // ================= 2. VIEW NAVIGATION & ROLE PERMISSIONS =================
    function applyRolePermissions() {
        const isAdmin = currentUser && (currentUser.role === 'Admin');
        document.querySelectorAll('.admin-only').forEach(el => {
            el.style.display = isAdmin ? '' : 'none';
        });
    }

    function switchView(viewName) {
        formLogin.classList.remove('active');
        formDashboard.classList.remove('active');
        formImageReg.classList.remove('active');

        navBtnDash.classList.remove('active');
        navBtnPic.classList.remove('active');
        navBtnUsers.classList.remove('active');

        applyRolePermissions();

        if (viewName === 'login') {
            formLogin.classList.add('active');
            userHeaderInfo.style.display = 'none';
        } else if (viewName === 'dashboard') {
            formDashboard.classList.add('active');
            navBtnDash.classList.add('active');
            userHeaderInfo.style.display = 'flex';
            checkDbStatus();
        } else if (viewName === 'image-reg') {
            formImageReg.classList.add('active');
            navBtnPic.classList.add('active');
            userHeaderInfo.style.display = 'flex';
            prepareImageRegistrationForm();
        }
    }

    navBtnDash.addEventListener('click', () => switchView('dashboard'));
    navBtnPic.addEventListener('click', () => switchView('image-reg'));
    btnHeroOpenPic.addEventListener('click', () => switchView('image-reg'));
    btnGotoPicForm.addEventListener('click', () => switchView('image-reg'));
    btnBackToDash.addEventListener('click', () => switchView('dashboard'));
    navBtnUsers.addEventListener('click', () => openUsersModal());
    btnGotoUsers.addEventListener('click', () => openUsersModal());

    // ================= 3. AUTHENTICATION & KEYBOARD NAVIGATION =================
    // Enter key navigation on login form
    usernameInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            passwordInput.focus();
            passwordInput.select();
        }
    });

    togglePasswordBtn.addEventListener('click', () => {
        if (passwordInput.type === 'password') {
            passwordInput.type = 'text';
            togglePasswordBtn.textContent = '🙈';
        } else {
            passwordInput.type = 'password';
            togglePasswordBtn.textContent = '👁️';
        }
    });

    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        loginAlert.style.display = 'none';

        const username = usernameInput.value.trim();
        const password = passwordInput.value.trim();

        if (!username || !password) {
            showAlert(loginAlert, 'تکایە ناوی بەکارهێنەر و وشەی نهێنی بنووسە.', 'danger');
            return;
        }

        const btnText = btnLoginSubmit.querySelector('.btn-text');
        const btnSpinner = btnLoginSubmit.querySelector('.btn-spinner');
        btnText.textContent = 'چاوەڕوانبە...';
        btnSpinner.style.display = 'inline-block';
        btnLoginSubmit.disabled = true;

        try {
            const res = await fetch('/api/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });

            const data = await res.json();

            if (data.success) {
                currentUser = data.user;
                headerUserName.textContent = currentUser.fullName || currentUser.username;
                headerUserRole.textContent = (currentUser.role === 'Admin') ? 'بەڕێوەبەر' : 'بەکارهێنەر';
                dashUserName.textContent = currentUser.fullName || currentUser.username;
                showAlert(loginAlert, data.message, 'success');

                setTimeout(() => {
                    switchView('image-reg'); // Go directly to image form for quick work!
                }, 400);
            } else {
                showAlert(loginAlert, data.message || 'ناوی بەکارهێنەر یان پاسۆرد هەڵەیە!', 'danger');
            }
        } catch (err) {
            showAlert(loginAlert, 'هەڵە لە پەیوەندی بە سێرڤەر: ' + err.message, 'danger');
        } finally {
            btnText.textContent = 'چوونەژوورەوە';
            btnSpinner.style.display = 'none';
            btnLoginSubmit.disabled = false;
        }
    });

    btnLogout.addEventListener('click', () => {
        if (confirm('دڵنیایت لە دەرچوون لە سیستەم؟')) {
            currentUser = null;
            usernameInput.value = '';
            passwordInput.value = '';
            loginAlert.style.display = 'none';
            switchView('login');
            setTimeout(() => usernameInput.focus(), 100);
        }
    });

    // ================= 4. IMAGE REGISTRATION FORM & ENTER KEY NAVIGATION =================
    async function prepareImageRegistrationForm() {
        // 1. Set today's date (Disabled for user editing)
        const today = new Date().toISOString().split('T')[0];
        picReceiptDateInput.value = today;

        // 2. Set logged in user name (Disabled for user editing)
        if (currentUser) {
            picRegisteredByInput.value = currentUser.fullName || currentUser.username;
        }

        // 3. Fetch Next Serial Number (Disabled for user editing)
        await fetchNextSerial();

        // 4. Load records table
        await loadImagesTable();

        // 5. Automatically focus Receipt No field
        setTimeout(() => {
            picReceiptNoInput.focus();
        }, 150);
    }

    async function fetchNextSerial() {
        try {
            const res = await fetch('/api/images/next-serial');
            const data = await res.json();
            if (data.success && data.nextSerial) {
                picSerialNoInput.value = data.nextSerial;
            }
        } catch (e) {
            console.error('Failed to get next serial', e);
        }
    }

    // --- ENTER KEY NAVIGATION WORKFLOW ---
    // User types Receipt No -> Press Enter -> Triggers Image File Selector!
    picReceiptNoInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            const val = picReceiptNoInput.value.trim();
            if (!val) {
                showAlert(receiptFormAlert, 'تکایە سەرەتا ژمارەی پسووڵە بنووسە پاشان Enter دابگرە.', 'danger');
                picReceiptNoInput.focus();
                return;
            }
            receiptFormAlert.style.display = 'none';

            // If image is not selected yet, automatically open file chooser dialog!
            if (!currentOptimizedImageBase64) {
                picFileInput.click();
            } else {
                // If image is already selected, focus save button
                btnSaveReceipt.focus();
            }
        }
    });

    // Browse image button click
    btnBrowseImage.addEventListener('click', () => picFileInput.click());
    imageDropzone.addEventListener('click', (e) => {
        if (e.target === imageDropzone || e.target.closest('#dropzone-empty-state')) {
            picFileInput.click();
        }
    });

    picFileInput.addEventListener('change', (e) => {
        if (e.target.files && e.target.files[0]) {
            processSelectedImage(e.target.files[0]);
        }
    });

    // Drag and Drop
    imageDropzone.addEventListener('dragover', (e) => {
        e.preventDefault();
        imageDropzone.classList.add('dragover');
    });

    imageDropzone.addEventListener('dragleave', () => {
        imageDropzone.classList.remove('dragover');
    });

    imageDropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        imageDropzone.classList.remove('dragover');
        if (e.dataTransfer.files && e.dataTransfer.files[0]) {
            processSelectedImage(e.dataTransfer.files[0]);
        }
    });

    // Paste image from Clipboard anywhere (Ctrl + V)
    document.addEventListener('paste', (e) => {
        if (!formImageReg.classList.contains('active')) return;
        const items = (e.clipboardData || e.originalEvent.clipboardData).items;
        for (let index in items) {
            const item = items[index];
            if (item.kind === 'file' && item.type.startsWith('image/')) {
                const blob = item.getAsFile();
                processSelectedImage(blob);
                break;
            }
        }
    });

    // Remove selected image
    btnRemoveImage.addEventListener('click', (e) => {
        e.stopPropagation();
        resetImagePicker();
        picReceiptNoInput.focus();
    });

    function resetImagePicker() {
        currentOptimizedImageBase64 = null;
        originalImageFile = null;
        picFileInput.value = '';
        dropzoneEmptyState.style.display = 'flex';
        dropzonePreviewState.style.display = 'none';
        previewImageElem.src = '';
        compressionMeter.style.display = 'none';
        qualityControls.style.display = 'none';
    }

    // Smart Canvas Image Optimizer (Lowest KB + Highest Quality)
    async function processSelectedImage(file) {
        if (!file.type.startsWith('image/')) {
            alert('تکایە تەنها وێنە هەڵبژێرە!');
            return;
        }

        originalImageFile = file;
        const origSizeKB = (file.size / 1024).toFixed(1);

        const reader = new FileReader();
        reader.onload = (event) => {
            const img = new Image();
            img.onload = () => {
                compressAndDisplayImage(img, origSizeKB);
                // Once image is processed, auto-focus Save Button for Enter-to-save!
                setTimeout(() => {
                    btnSaveReceipt.focus();
                }, 150);
            };
            img.src = event.target.result;
        };
        reader.readAsDataURL(file);
    }

    function compressAndDisplayImage(img, origSizeKB) {
        // High Quality Dimension Capping: Max 1920 width / 1080 height (Full HD)
        const maxWidth = 1920;
        const maxHeight = 1080;
        let width = img.width;
        let height = img.height;

        if (width > maxWidth || height > maxHeight) {
            if (width / height > maxWidth / maxHeight) {
                height = Math.round((height * maxWidth) / width);
                width = maxWidth;
            } else {
                width = Math.round((width * maxHeight) / height);
                maxHeight;
            }
        }

        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');

        ctx.imageSmoothingEnabled = true;
        ctx.imageSmoothingQuality = 'high';
        ctx.drawImage(img, 0, 0, width, height);

        // Convert to optimized JPEG with quality 0.82
        const optBase64 = canvas.toDataURL('image/jpeg', compressionQuality);
        currentOptimizedImageBase64 = optBase64;

        // Calculate size in KB
        const optSizeBytes = Math.round((optBase64.length * 3) / 4);
        const optSizeKB = (optSizeBytes / 1024).toFixed(1);

        // Show preview
        previewImageElem.src = optBase64;
        dropzoneEmptyState.style.display = 'none';
        dropzonePreviewState.style.display = 'flex';

        // Update Compression Meter
        meterOrigSize.textContent = `${origSizeKB} کیلۆبایت`;
        meterOptSize.textContent = `${optSizeKB} کیلۆبایت`;

        const savingsPercent = Math.max(0, Math.round(((origSizeKB - optSizeKB) / origSizeKB) * 100));
        meterSavings.textContent = `کەمکراوەتەوە بە ڕێژەی ${savingsPercent}٪`;

        compressionMeter.style.display = 'flex';
        qualityControls.style.display = 'flex';
    }

    // Quality slider
    qualitySlider.addEventListener('input', (e) => {
        compressionQuality = parseFloat(e.target.value);
        qualityValText.textContent = (compressionQuality >= 0.8) ? 'کوالێتی بەرز' : 'کوالێتی ئاسایی';
        if (originalImageFile) {
            processSelectedImage(originalImageFile);
        }
    });

    btnEnlargePreview.addEventListener('click', (e) => {
        e.stopPropagation();
        if (currentOptimizedImageBase64) {
            const previewData = {
                imageSrc: currentOptimizedImageBase64,
                receiptNo: picReceiptNoInput.value.trim() || 'نوێ',
                serialNo: picSerialNoInput.value.trim() || '-',
                receiptDate: picReceiptDateInput.value,
                registeredBy: picRegisteredByInput.value || (currentUser ? currentUser.fullName : ''),
                imageSizeKB: meterOptSize.textContent
            };
            openLightbox(
                currentOptimizedImageBase64,
                `پێشبینینی وێنەی پسووڵە (#${previewData.receiptNo})`,
                `قەبارە: ${meterOptSize.textContent} | بەروار: ${previewData.receiptDate}`,
                previewData
            );
        }
    });

    // Form Submit (Save to Database)
    receiptImageForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        receiptFormAlert.style.display = 'none';

        const serialNo = picSerialNoInput.value.trim();
        const receiptNo = picReceiptNoInput.value.trim();
        const receiptDate = picReceiptDateInput.value;
        const registeredBy = picRegisteredByInput.value || (currentUser ? currentUser.fullName : 'بەڕێوەبەر');

        if (!receiptNo) {
            showAlert(receiptFormAlert, 'تکایە ژمارەی پسووڵە بنووسە.', 'danger');
            picReceiptNoInput.focus();
            return;
        }

        if (!currentOptimizedImageBase64) {
            showAlert(receiptFormAlert, 'تکایە وێنەی پسووڵەکە دیاری بکە یان دایبنێ.', 'danger');
            picFileInput.click();
            return;
        }

        btnSaveReceipt.disabled = true;
        btnSaveReceipt.innerHTML = '<span>⏳ پاشەکەوتکردن لە سێرڤەر...</span>';

        try {
            const res = await fetch('/api/images', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    serialNo,
                    receiptNo,
                    receiptDate,
                    imageBase64: currentOptimizedImageBase64,
                    registeredBy
                })
            });

            const data = await res.json();

            if (data.success) {
                showAlert(receiptFormAlert, `پسووڵەی ژمارە (${receiptNo}) بە سەرکەوتوویی تۆمارکرا (زنجیرە: ${data.record.serialNo})`, 'success');
                
                // Reset inputs for next receipt
                picReceiptNoInput.value = '';
                resetImagePicker();
                await fetchNextSerial();
                await loadImagesTable();
                checkDbStatus();

                // Auto-focus Receipt No for seamless next entry!
                setTimeout(() => {
                    picReceiptNoInput.focus();
                }, 200);
            } else {
                showAlert(receiptFormAlert, data.message || 'هەڵە لە تۆمارکردن!', 'danger');
                btnSaveReceipt.focus();
            }
        } catch (err) {
            showAlert(receiptFormAlert, 'هەڵە لە پەیوەندی بە سێرڤەر: ' + err.message, 'danger');
        } finally {
            btnSaveReceipt.disabled = false;
            btnSaveReceipt.innerHTML = '<span>💾 تۆمارکردن لە داتابەیس (Enter)</span>';
        }
    });

    btnResetReceiptForm.addEventListener('click', () => {
        picReceiptNoInput.value = '';
        resetImagePicker();
        fetchNextSerial();
        receiptFormAlert.style.display = 'none';
        picReceiptNoInput.focus();
    });

    // ================= 5. RECORDS ARCHIVE TABLE =================
    const tableMainTitle = document.getElementById('table-main-title');
    const tableSubtitleInfo = document.getElementById('table-subtitle-info');

    async function loadImagesTable(searchTerm = '') {
        imagesTableBody.innerHTML = '<tr><td colspan="8" style="text-align: center;">چاوەڕوانبە لە هێنانی داتاکان...</td></tr>';
        try {
            const url = searchTerm ? `/api/images?search=${encodeURIComponent(searchTerm)}` : '/api/images';
            const res = await fetch(url);
            const data = await res.json();

            if (data.success && data.images) {
                // Update table titles based on search or today-only
                if (searchTerm) {
                    if (tableMainTitle) tableMainTitle.textContent = `🔍 ئەنجامی گەڕان لە هەموو ئەرشیفدا (${data.images.length} پسووڵە)`;
                    if (tableSubtitleInfo) tableSubtitleInfo.textContent = `ئەنجامی گەڕان بەپێی: "${searchTerm}" لە تەواوی مێژووی تەیبڵەکە`;
                } else {
                    if (tableMainTitle) tableMainTitle.textContent = `📋 وێنە و پسووڵەکانی ئەمڕۆ (${data.images.length} پسووڵە)`;
                    if (tableSubtitleInfo) tableSubtitleInfo.textContent = `تەنها تۆمارەکانی ئەمڕۆ پیشان دەدرێن بۆ کەمکردنەوەی لۆدی سێرڤەر (بۆ بەروارەکانی تر، گەڕان بەکاربهێنە)`;
                }

                if (data.images.length === 0) {
                    const emptyMsg = searchTerm ? 'هیچ پسووڵەیەک نەدۆزرایەوە بەم ناو یان ژمارەیە' : 'ئەمڕۆ تا ئێستا هیچ پسووڵەیەک تۆمار نەکراوە';
                    imagesTableBody.innerHTML = `<tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 24px;">${emptyMsg}</td></tr>`;
                    return;
                }

                imagesTableBody.innerHTML = '';
                const isAdmin = currentUser && (currentUser.role === 'Admin');

                data.images.forEach((item) => {
                    const row = document.createElement('tr');
                    const timeFormatted = formatClockTime(item.CreatedTime, item.CreatedAtLocal || item.CreatedAt);
                    
                    row.innerHTML = `
                        <td><strong>${item.SerialNo}</strong></td>
                        <td><span style="font-weight:700; color:var(--primary);">${item.ReceiptNo}</span></td>
                        <td>${item.ReceiptDate}</td>
                        <td>${timeFormatted}</td>
                        <td>
                            <img src="${item.ImagePath}" alt="وێنە" class="table-thumb-img" data-full="${item.ImagePath}" data-receipt="${item.ReceiptNo}" data-serial="${item.SerialNo}" data-date="${item.ReceiptDate}" data-time="${item.CreatedTime || ''}" data-user="${item.RegisteredBy}" data-size="${item.ImageSizeKB}">
                        </td>
                        <td><span class="kb-badge">${item.ImageSizeKB ? item.ImageSizeKB.toFixed(1) : '-'} کیلۆبایت</span></td>
                        <td>👤 ${item.RegisteredBy}</td>
                        <td>
                            <div class="actions-cell-flex">
                                <button class="btn-action-icon view-img-btn" title="پیشاندانی گەورە">🔍</button>
                                <button class="btn-action-icon print print-img-btn" title="چاپکردنی پسووڵە (A5)">🖨️</button>
                                <a href="${item.ImagePath}" download="receipt_${item.ReceiptNo}.jpg" class="btn-action-icon" title="دابەزاندن">⬇</a>
                                ${isAdmin ? `<button class="btn-action-icon delete delete-img-btn" data-id="${item.Id}" data-receipt="${item.ReceiptNo}" title="سڕینەوە">🗑️</button>` : ''}
                            </div>
                        </td>
                    `;
                    imagesTableBody.appendChild(row);
                });

                // Attach Thumbnail & View Click Handlers
                imagesTableBody.querySelectorAll('.table-thumb-img, .view-img-btn').forEach(elem => {
                    elem.addEventListener('click', (e) => {
                        const row = e.target.closest('tr');
                        const img = row.querySelector('.table-thumb-img');
                        const rowData = {
                            imageSrc: img.getAttribute('data-full'),
                            receiptNo: img.getAttribute('data-receipt'),
                            serialNo: img.getAttribute('data-serial'),
                            receiptDate: img.getAttribute('data-date'),
                            createdTime: img.getAttribute('data-time'),
                            registeredBy: img.getAttribute('data-user'),
                            imageSizeKB: img.getAttribute('data-size')
                        };
                        const timeText = rowData.createdTime ? ` | کات: ${rowData.createdTime}` : '';
                        openLightbox(
                            rowData.imageSrc,
                            `وێنەی پسووڵەی ژمارە (${rowData.receiptNo}) - زنجیرە: ${rowData.serialNo}`,
                            `بەرواری پسووڵە: ${rowData.receiptDate}${timeText} | تۆمارکراوە لەلایەن: ${rowData.registeredBy} | قەبارە: ${rowData.imageSizeKB} کیلۆبایت`,
                            rowData
                        );
                    });
                });

                // Attach Table Print Click Handlers
                imagesTableBody.querySelectorAll('.print-img-btn').forEach(btn => {
                    btn.addEventListener('click', (e) => {
                        const row = e.target.closest('tr');
                        const img = row.querySelector('.table-thumb-img');
                        const rowData = {
                            imageSrc: img.getAttribute('data-full'),
                            receiptNo: img.getAttribute('data-receipt'),
                            serialNo: img.getAttribute('data-serial'),
                            receiptDate: img.getAttribute('data-date'),
                            createdTime: img.getAttribute('data-time'),
                            registeredBy: img.getAttribute('data-user'),
                            imageSizeKB: img.getAttribute('data-size')
                        };
                        printReceiptA5(rowData);
                    });
                });

                // Attach Delete Handlers (Only available if Admin)
                if (isAdmin) {
                    imagesTableBody.querySelectorAll('.delete-img-btn').forEach(btn => {
                        btn.addEventListener('click', async (e) => {
                            const id = e.target.getAttribute('data-id');
                            const receipt = e.target.getAttribute('data-receipt');
                            if (confirm(`دڵنیایت لە سڕینەوەی پسووڵەی ژمارە (${receipt})؟`)) {
                                await deleteImageRecord(id);
                            }
                        });
                    });
                }
            }
        } catch (err) {
            imagesTableBody.innerHTML = `<tr><td colspan="8" style="color:red; text-align:center;">هەڵە لە هێنانی زانیاریەکان: ${err.message}</td></tr>`;
        }
    }

    async function deleteImageRecord(id) {
        if (!currentUser || currentUser.role !== 'Admin') {
            alert('تەنها بەڕێوەبەری سەرەکی (Admin) دەسەڵاتی سڕینەوەی وێنە و پسووڵەکانی هەیە.');
            return;
        }

        try {
            const res = await fetch(`/api/images/${id}`, { method: 'DELETE' });
            const data = await res.json();
            if (data.success) {
                await loadImagesTable(inputSearchImages.value.trim());
                checkDbStatus();
            } else {
                alert(data.message);
            }
        } catch (err) {
            alert('هەڵە لە سڕینەوە: ' + err.message);
        }
    }

    // Search filter
    let searchTimeout = null;
    inputSearchImages.addEventListener('input', () => {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            loadImagesTable(inputSearchImages.value.trim());
        }, 300);
    });

    btnRefreshImages.addEventListener('click', () => {
        inputSearchImages.value = '';
        loadImagesTable('');
    });

    // ================= 6. LIGHTBOX MODAL & A5 RECEIPT PRINTING =================
    function openLightbox(src, title, info, receiptData = null) {
        lightboxImg.src = src;
        lightboxTitle.textContent = title;
        lightboxDownloadLink.href = src;
        lightboxInfoBar.textContent = info;
        currentReceiptPrintData = receiptData || {
            imageSrc: src,
            receiptNo: '',
            serialNo: '',
            receiptDate: '',
            registeredBy: '',
            imageSizeKB: ''
        };
        modalLightbox.style.display = 'flex';
    }

    btnCloseLightbox.addEventListener('click', () => {
        modalLightbox.style.display = 'none';
        lightboxImg.src = '';
        currentReceiptPrintData = null;
    });

    if (btnLightboxPrint) {
        btnLightboxPrint.addEventListener('click', () => {
            if (currentReceiptPrintData) {
                printReceiptA5(currentReceiptPrintData);
            } else if (lightboxImg.src) {
                printReceiptA5({
                    imageSrc: lightboxImg.src,
                    receiptNo: '',
                    serialNo: '',
                    receiptDate: '',
                    registeredBy: '',
                    imageSizeKB: ''
                });
            }
        });
    }

    // ================= A5 RECEIPT PRINTING FUNCTION =================
    function printReceiptA5(data) {
        if (!data || !data.imageSrc) {
            alert('وێنەی پسووڵە بەردەست نییە بۆ چاپکردن!');
            return;
        }

        // Remove any old printing iframe
        const oldIframe = document.getElementById('receipt-print-iframe');
        if (oldIframe) {
            oldIframe.remove();
        }

        const iframe = document.createElement('iframe');
        iframe.id = 'receipt-print-iframe';
        iframe.style.position = 'fixed';
        iframe.style.right = '0';
        iframe.style.bottom = '0';
        iframe.style.width = '0';
        iframe.style.height = '0';
        iframe.style.border = '0';
        iframe.style.zIndex = '-9999';
        document.body.appendChild(iframe);

        const printDoc = iframe.contentWindow.document;
        const printDateFormatted = new Date().toLocaleString('ku');

        const html = `
<!DOCTYPE html>
<html lang="ckb" dir="rtl">
<head>
    <meta charset="UTF-8">
    <title></title>
    <style>
        @page {
            size: A5 portrait;
            margin: 0 !important;
        }
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
        }
        html, body {
            width: 148mm;
            height: 210mm;
            margin: 0 !important;
            padding: 0 !important;
            background: #ffffff;
            color: #0f172a;
            font-family: 'Segoe UI', Tahoma, Arial, sans-serif;
            direction: rtl;
            overflow: hidden;
        }
        .a5-container {
            width: 148mm;
            height: 210mm;
            box-sizing: border-box;
            padding: 5mm 6mm;
            margin: 0 auto;
            display: flex;
            flex-direction: column;
            justify-content: flex-start;
            page-break-inside: avoid;
            page-break-after: avoid;
            overflow: hidden;
        }
        .receipt-header-card {
            border: 2px solid #0f172a;
            border-radius: 6px;
            padding: 5px 8px;
            background: #f8fafc;
        }
        .meta-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 5px;
        }
        .meta-box {
            background: #ffffff;
            border: 1px solid #cbd5e1;
            padding: 3px 6px;
            border-radius: 4px;
            text-align: right;
        }
        .meta-label {
            font-size: 7.5pt;
            color: #64748b;
            display: block;
            margin-bottom: 1px;
            font-weight: 600;
        }
        .meta-value {
            font-size: 9pt;
            font-weight: 800;
            color: #0f172a;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }
        .image-frame {
            width: 100%;
            height: 174mm;
            max-height: 174mm;
            border: 1.5px solid #0f172a;
            border-radius: 6px;
            background: #ffffff;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow: hidden;
            margin-top: 4mm;
            box-sizing: border-box;
        }
        .receipt-image {
            max-width: 100%;
            max-height: 172mm;
            object-fit: contain;
            display: block;
            margin: 0 auto;
        }
    </style>
</head>
<body>
    <div class="a5-container">
        <!-- Header: Only essential receipt details -->
        <div class="receipt-header-card">
            <div class="meta-grid">
                <div class="meta-box">
                    <span class="meta-label">ژمارەی پسووڵە:</span>
                    <span class="meta-value" style="color: #0284c7;">${data.receiptNo || '-'}</span>
                </div>
                <div class="meta-box">
                    <span class="meta-label">زنجیرە:</span>
                    <span class="meta-value">${data.serialNo || '-'}</span>
                </div>
                <div class="meta-box">
                    <span class="meta-label">بەرواری پسووڵە:</span>
                    <span class="meta-value">${data.receiptDate || '-'}</span>
                </div>
                <div class="meta-box">
                    <span class="meta-label">تۆمارکراوە لەلایەن:</span>
                    <span class="meta-value">${data.registeredBy || '-'}</span>
                </div>
            </div>
        </div>

        <!-- Receipt Image (clean and filling full A5 / half A4 with NO text underneath) -->
        <div class="image-frame">
            <img id="print-receipt-image" src="${data.imageSrc}" class="receipt-image" alt="وێنەی پسووڵە">
        </div>
    </div>
</body>
</html>
        `;

        printDoc.open();
        printDoc.write(html);
        printDoc.close();

        const triggerPrint = () => {
            try {
                iframe.contentWindow.focus();
                iframe.contentWindow.print();
            } catch (err) {
                console.error('Print trigger error:', err);
            }
        };

        const img = printDoc.getElementById('print-receipt-image');
        if (img) {
            if (img.complete && img.naturalHeight > 0) {
                setTimeout(triggerPrint, 250);
            } else {
                img.onload = () => setTimeout(triggerPrint, 250);
                img.onerror = () => setTimeout(triggerPrint, 250);
            }
        } else {
            setTimeout(triggerPrint, 300);
        }
    }

    // ================= 7. USER MANAGEMENT MODAL (ADMIN ONLY) =================
    function openUsersModal() {
        if (!currentUser || currentUser.role !== 'Admin') {
            alert('تەنها بەڕێوەبەری سەرەکی دەسەڵاتی بینین و بەڕێوەبردنی بەکارهێنەرانی هەیە.');
            return;
        }
        modalUsers.style.display = 'flex';
        loadUsersTable();
    }

    function closeUsersModal() {
        modalUsers.style.display = 'none';
        addUserAlert.style.display = 'none';
        addUserForm.reset();
        checkDbStatus();
    }

    btnCloseUsersModal.addEventListener('click', closeUsersModal);

    async function loadUsersTable() {
        usersTableBody.innerHTML = '<tr><td colspan="6" style="text-align: center;">چاوەڕوانبە...</td></tr>';
        try {
            const res = await fetch('/api/users');
            const data = await res.json();

            if (data.success && data.users) {
                statTotalUsers.textContent = data.users.length;
                if (data.users.length === 0) {
                    usersTableBody.innerHTML = '<tr><td colspan="6" style="text-align: center;">هیچ بەکارهێنەرێک نەدۆزرایەوە</td></tr>';
                    return;
                }

                usersTableBody.innerHTML = '';
                data.users.forEach((u, index) => {
                    const row = document.createElement('tr');
                    const createdDate = u.CreatedAt ? new Date(u.CreatedAt).toLocaleDateString('ku') : '-';
                    const roleKu = (u.Role === 'Admin') ? 'بەڕێوەبەر' : 'بەکارهێنەر';

                    row.innerHTML = `
                        <td>${index + 1}</td>
                        <td><strong>${u.FullName}</strong></td>
                        <td><code>${u.Username}</code></td>
                        <td><span class="role-tag">${roleKu}</span></td>
                        <td>${createdDate}</td>
                        <td>
                            ${u.Id !== 1 ? `<button class="btn-delete-user" data-id="${u.Id}">سڕینەوە</button>` : '<small style="color:var(--text-muted)">سەرەکی</small>'}
                        </td>
                    `;
                    usersTableBody.appendChild(row);
                });

                document.querySelectorAll('.btn-delete-user').forEach(btn => {
                    btn.addEventListener('click', async (e) => {
                        const id = e.target.getAttribute('data-id');
                        if (confirm('دڵنیایت لە سڕینەوەی ئەم بەکارهێنەرە؟')) {
                            await deleteUser(id);
                        }
                    });
                });
            }
        } catch (err) {
            usersTableBody.innerHTML = `<tr><td colspan="6" style="color:red; text-align:center;">هەڵە: ${err.message}</td></tr>`;
        }
    }

    addUserForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const fullName = document.getElementById('new-fullname').value.trim();
        const username = document.getElementById('new-username').value.trim();
        const password = document.getElementById('new-password').value.trim();
        const role = document.getElementById('new-role').value;

        try {
            const res = await fetch('/api/users', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ fullName, username, password, role })
            });
            const data = await res.json();

            if (data.success) {
                showAlert(addUserAlert, data.message, 'success');
                addUserForm.reset();
                loadUsersTable();
            } else {
                showAlert(addUserAlert, data.message, 'danger');
            }
        } catch (err) {
            showAlert(addUserAlert, 'هەڵە لە زیادکردن: ' + err.message, 'danger');
        }
    });

    async function deleteUser(id) {
        try {
            const res = await fetch(`/api/users/${id}`, { method: 'DELETE' });
            const data = await res.json();
            if (data.success) {
                loadUsersTable();
            } else {
                alert(data.message);
            }
        } catch (err) {
            alert('هەڵە لە سڕینەوە: ' + err.message);
        }
    }

    // ================= 8. DATABASE INFO MODAL (ADMIN ONLY) =================
    btnShowDbDetails.addEventListener('click', () => {
        if (!currentUser || currentUser.role !== 'Admin') {
            alert('تەنها بەڕێوەبەری سەرەکی دەسەڵاتی بینینی زانیاری سێرڤەری هەیە.');
            return;
        }
        modalDbInfo.style.display = 'flex';
    });

    btnCloseDbModal.addEventListener('click', () => {
        modalDbInfo.style.display = 'none';
    });

    btnRetestDb.addEventListener('click', async () => {
        await checkDbStatus();
        alert('پشکنینی پەیوەندی بە سەرکەوتوویی ئەنجامدرا!');
    });

    // Close Modals on backdrop click
    window.addEventListener('click', (e) => {
        if (e.target === modalUsers) closeUsersModal();
        if (e.target === modalDbInfo) modalDbInfo.style.display = 'none';
        if (e.target === modalLightbox) {
            modalLightbox.style.display = 'none';
            lightboxImg.src = '';
        }
    });

    function showAlert(elem, msg, type) {
        elem.className = `alert-box ${type}`;
        elem.textContent = msg;
        elem.style.display = 'block';
    }

    // Initial check
    checkDbStatus();
    setInterval(checkDbStatus, 15000);
});
