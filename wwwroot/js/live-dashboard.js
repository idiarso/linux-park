// Global variables
let entryScanner = null;
let exitScanner = null;
let entryCameraStream = null;
let exitCameraStream = null;
let selectedEntryCameraId = null;
let selectedExitCameraId = null;
let isAutoEntryEnabled = true;
let isAutoExitEnabled = true;
let reconnectAttempts = 0;
const MAX_RECONNECT_ATTEMPTS = 5;

// Initialize DataTables
const recentEntriesTable = $('#recentEntriesTable').DataTable({
    order: [[0, 'desc']],
    language: {
        url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/id.json'
    },
    pageLength: 5
});

const recentExitsTable = $('#recentExitsTable').DataTable({
    order: [[0, 'desc']],
    language: {
        url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/id.json'
    },
    pageLength: 5
});

// SignalR Connection
let connection;

document.addEventListener('DOMContentLoaded', function() {
    // Initialize SignalR
    initializeSignalR();
    
    // Add event listeners
    initPushButton();
    
    // Initialize filter buttons
    initializeFilters();
    
    // Make entire cards clickable for better mobile experience
    makeCardsClickable();
});

function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/parkingHub")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 15000, 30000]) // Retry quickly at first, then with increasing delays
        .configureLogging(signalR.LogLevel.Information)
        .build();
    
    // Connect to hub
    connection.start()
        .then(() => {
            console.log('SignalR connected');
            showToast('success', 'Connected to server');
            addToSimulationLog('Connected to server', 'success');
            reconnectAttempts = 0;
            
            // Register handlers after connected
            registerSignalRHandlers();
        })
        .catch(err => {
            console.error('Error connecting to SignalR hub', err);
            addToSimulationLog('Connection error: ' + err.message, 'error');
            
            // Retry connection with exponential backoff
            if (reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
                reconnectAttempts++;
                const delay = Math.min(1000 * Math.pow(2, reconnectAttempts), 30000);
                addToSimulationLog(`Reconnecting in ${delay/1000} seconds... (attempt ${reconnectAttempts}/${MAX_RECONNECT_ATTEMPTS})`, 'warning');
                
                setTimeout(() => {
                    initializeSignalR();
                }, delay);
            } else {
                addToSimulationLog('Max reconnection attempts reached. Please refresh the page.', 'error');
                showErrorAlert('Connection Failed', 'Failed to connect to server after multiple attempts. Please refresh the page.');
            }
        });
        
    // Handle reconnection events
    connection.onreconnecting(error => {
        console.log('Connection lost, reconnecting...', error);
        addToSimulationLog('Connection lost, reconnecting...', 'warning');
    });
    
    connection.onreconnected(connectionId => {
        console.log('Reconnected with connection ID:', connectionId);
        addToSimulationLog('Reconnected to server', 'success');
        showToast('success', 'Reconnected to server');
    });
    
    connection.onclose(error => {
        console.log('Connection closed', error);
        addToSimulationLog('Connection closed', 'error');
    });
}

function initPushButton() {
    // Add push button simulator
    const pushButton = document.querySelector('.simulate-push-button');
    if (pushButton) {
        pushButton.addEventListener('click', function() {
            simulatePushButton();
        });
    }
}

function initializeFilters() {
    // Activity filter buttons
    const filterButtons = document.querySelectorAll('[onclick^="filterActivities"]');
    if (filterButtons.length > 0) {
        filterButtons.forEach(button => {
            button.addEventListener('click', function() {
                // Remove active class from all buttons
                filterButtons.forEach(b => b.classList.remove('active'));
                // Add active class to clicked button
                this.classList.add('active');
            });
        });
    }
}

function makeCardsClickable() {
    // Make action cards fully clickable
    const actionCards = document.querySelectorAll('.action-card');
    actionCards.forEach(card => {
        const button = card.querySelector('button');
        if (button) {
            card.style.cursor = 'pointer';
            card.addEventListener('click', function(e) {
                // Prevent clicking if the actual button was clicked
                if (e.target === button || button.contains(e.target)) {
                    return;
                }
                // Trigger button click
                button.click();
            });
        }
    });
}

function simulatePushButton() {
    if (!connection) {
        console.error('SignalR connection not initialized');
        showToast('error', 'Connection not initialized. Please refresh the page.');
        addToSimulationLog('Error: Connection not initialized', 'error');
        return;
    }
    
    if (connection.state !== signalR.HubConnectionState.Connected) {
        console.error('SignalR not connected, current state:', connection.state);
        showToast('error', 'Not connected to server. Please wait for reconnection or refresh the page.');
        addToSimulationLog('Error: Not connected to server (state: ' + connection.state + ')', 'error');
        return;
    }
    
    console.log('Simulating push button...');
    
    const entryPoint = "ENTRY1"; // Default entry point
    
    // Show spinner on button
    const btn = document.querySelector('.simulate-push-button');
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
    
    // Add to simulation log immediately
    addToSimulationLog('Sending push button event...', 'info');
    
    // Call the hub method
    connection.invoke("PushButtonPressed", entryPoint)
        .then(() => {
            console.log('Push button simulation sent successfully');
            showToast('success', 'Push button simulation sent successfully');
            
            // Add to simulation log
            addToSimulationLog('Push button event sent successfully', 'success');
        })
        .catch(err => {
            console.error('Error simulating push button:', err);
            showToast('error', 'Error simulating push button: ' + (err.message || 'Unknown error'));
            
            // Add to simulation log
            addToSimulationLog('Error: ' + (err.message || 'Unknown error'), 'error');
        })
        .finally(() => {
            // Reset button
            setTimeout(() => {
                btn.disabled = false;
                btn.innerHTML = originalText;
            }, 2000);
        });
}

function registerSignalRHandlers() {
    // Handle receipt of push button event
    connection.on('PushButtonEvent', function(entryPoint) {
        console.log('Push button pressed at', entryPoint);
        addToSimulationLog(`Push button pressed at gate ${entryPoint}`);
        showToast('info', `Push button pressed at gate ${entryPoint}`);
    });
    
    // Handle receipt of entry complete
    connection.on('EntryComplete', function(data) {
        console.log('Entry complete:', data);
        addToSimulationLog(`Vehicle ${data.vehicleNumber} entered with ticket ${data.ticketNumber}`, 'success');
        showToast('success', `Vehicle ${data.vehicleNumber} entered successfully`);
        
        // Play success sound
        playSound('success');
    });
    
    // Handle receipt of entry error
    connection.on('EntryError', function(error) {
        console.error('Entry error:', error);
        addToSimulationLog(`Entry error: ${error}`, 'error');
        showToast('error', error);
        
        // Play error sound
        playSound('error');
    });
    
    // Handle receipt of gate open/close
    connection.on('OpenEntryGate', function(gateId) {
        console.log('Gate opened:', gateId);
        addToSimulationLog(`Gate ${gateId} opened`);
        showToast('info', `Gate ${gateId} opened`);
        
        // After delay, close gate
        setTimeout(() => {
            addToSimulationLog(`Gate ${gateId} closed automatically (after timeout)`);
            showToast('info', `Gate ${gateId} closed automatically`);
        }, 5000);
    });
    
    // Handle dashboard updates
    connection.on('UpdateDashboard', function(data) {
        console.log('Dashboard updated:', data);
        updateDashboardUI(data);
    });
    
    // Handle any errors
    connection.on('Error', function(error) {
        console.error('SignalR error:', error);
        addToSimulationLog(`Error: ${error}`, 'error');
        showToast('error', error);
    });
}

function updateDashboardUI(data) {
    // Update total spaces
    const totalSpacesEl = document.querySelector('.total-spaces');
    if (totalSpacesEl) totalSpacesEl.textContent = data.totalSpaces;
    
    // Update available spaces
    const availableSpacesEl = document.querySelector('.available-spaces');
    if (availableSpacesEl) availableSpacesEl.textContent = data.availableSpaces;
    
    // Update occupied spaces
    const occupiedSpacesEl = document.querySelector('.occupied-spaces');
    if (occupiedSpacesEl) occupiedSpacesEl.textContent = data.occupiedSpaces;
    
    // Update today's revenue
    const todayRevenueEl = document.querySelector('.today-revenue');
    if (todayRevenueEl) {
        const formatter = new Intl.NumberFormat('id-ID');
        todayRevenueEl.textContent = `Rp ${formatter.format(data.todayRevenue)}`;
    }
    
    // Update activity table
    if (data.recentActivities && data.recentActivities.length > 0) {
        const tbody = document.getElementById('activityTableBody');
        if (tbody) {
            tbody.innerHTML = data.recentActivities.map(activity => `
                <tr class="activity-row ${activity.status.toLowerCase()}-activity">
                    <td>${activity.time}</td>
                    <td>${activity.vehicleNumber}</td>
                    <td>${activity.vehicleType}</td>
                    <td>
                        <span class="badge bg-${activity.status === 'Exit' ? 'danger' : 'success'}">
                            ${activity.status}
                        </span>
                    </td>
                    <td>
                        <button class="btn btn-sm btn-info" onclick="viewDetails('${activity.vehicleNumber}')">
                            <i class="fas fa-info-circle"></i>
                        </button>
                    </td>
                </tr>
            `).join('');
        }
    }
}

function filterActivities(type) {
    const rows = document.querySelectorAll('.activity-row');
    if (rows.length === 0) return;
    
    rows.forEach(row => {
        if (type === 'all' || row.classList.contains(`${type}-activity`)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function addToSimulationLog(message, type = 'info') {
    const logEl = document.getElementById('simulation-log');
    if (!logEl) return;
    
    const timestamp = new Date().toLocaleTimeString();
    const logEntry = document.createElement('p');
    logEntry.className = `mb-1 text-${type === 'error' ? 'danger' : type === 'success' ? 'success' : type === 'warning' ? 'warning' : 'dark'}`;
    logEntry.innerHTML = `<small>[${timestamp}]</small> ${message}`;
    
    logEl.prepend(logEntry);
    
    // Limit log size
    if (logEl.children.length > 50) {
        logEl.removeChild(logEl.lastChild);
    }
}

function showToast(type, message) {
    // Create toast container if it doesn't exist
    let toastContainer = document.querySelector('.toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // Create toast
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'primary'} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    toast.setAttribute('id', toastId);
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    toastContainer.appendChild(toast);
    
    // Show toast
    const bsToast = new bootstrap.Toast(toast, {
        delay: 3000
    });
    bsToast.show();
    
    // Remove toast after it's hidden
    toast.addEventListener('hidden.bs.toast', function () {
        toast.remove();
    });
}

function playSound(type) {
    try {
        const audio = new Audio(`/sounds/${type}.mp3`);
        audio.play().catch(err => console.error('Error playing sound:', err));
    } catch (err) {
        console.error('Error creating audio:', err);
    }
}

function showErrorAlert(title, message) {
    // Create a modal to show the error
    const modalId = 'errorModal-' + Date.now();
    const modalHTML = `
        <div class="modal fade" id="${modalId}" tabindex="-1" aria-labelledby="${modalId}-label" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header bg-danger text-white">
                        <h5 class="modal-title" id="${modalId}-label">${title}</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        ${message}
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        <button type="button" class="btn btn-primary" onclick="window.location.reload()">Refresh Page</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Append modal to body
    const modalEl = document.createElement('div');
    modalEl.innerHTML = modalHTML;
    document.body.appendChild(modalEl.firstChild);
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById(modalId));
    modal.show();
}

// Export functions for global use
window.simulatePushButton = simulatePushButton;
window.filterActivities = filterActivities;
window.viewDetails = function(vehicleNumber) {
    // Navigate to vehicle details page
    window.location.href = `/Vehicle/Details?vehicleNumber=${encodeURIComponent(vehicleNumber)}`;
};

// Auto switches handlers
document.getElementById('autoEntrySwitch').addEventListener('change', function() {
    isAutoEntryEnabled = this.checked;
});

document.getElementById('autoExitSwitch').addEventListener('change', function() {
    isAutoExitEnabled = this.checked;
});

// Initialize entry camera
const maxRetries = 3;
let retryCount = 0;
let retryDelay = 1000; // Start with 1 second delay
let isReconnecting = false;

async function initializeEntryCamera() {
    try {
        const video = document.getElementById('entryCamera');
        if (!video) {
            throw new Error('Camera element not found');
        }

        // IP Camera Configuration
        const ipCameraUrl = 'http://192.168.186.200:8000/stream';
        console.log('Menghubungkan ke kamera IP:', ipCameraUrl);

        if (Hls.isSupported()) {
            const hls = new Hls({
                debug: false,
                enableWorker: true,
                lowLatencyMode: true,
                backBufferLength: 90
            });

            hls.loadSource(ipCameraUrl);
            hls.attachMedia(video);

            hls.on(Hls.Events.MANIFEST_PARSED, function() {
                video.play().catch(e => {
                    console.error('Error playing video:', e);
                });
                retryCount = 0;
                retryDelay = 1000;
                isReconnecting = false;
            });

            hls.on(Hls.Events.ERROR, function(event, data) {
                if (data.fatal) {
                    switch(data.type) {
                        case Hls.ErrorTypes.NETWORK_ERROR:
                            handleNetworkError(hls);
                            break;
                        case Hls.ErrorTypes.MEDIA_ERROR:
                            handleMediaError(hls);
                            break;
                        default:
                            handleFatalError(hls);
                            break;
                    }
                }
            });
        } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
            video.src = ipCameraUrl;
            video.addEventListener('loadedmetadata', function() {
                video.play().catch(e => {
                    console.error('Error playing video:', e);
                    handleFatalError();
                });
            });
        } else {
            throw new Error('HLS tidak didukung oleh browser ini');
        }

        entryCameraStream = video.srcObject;
        startMotionDetection();
    } catch (error) {
        console.error('Error mengakses kamera:', error);
        showErrorAlert('Error', 'Gagal mengakses kamera: ' + error.message);
    }
}

function handleNetworkError(hls) {
    if (!isReconnecting && retryCount < maxRetries) {
        isReconnecting = true;
        console.log(`Error jaringan, mencoba ulang ${retryCount + 1} dari ${maxRetries} dalam ${retryDelay/1000} detik...`);
        
        showReconnectingAlert(retryCount + 1);
        
        setTimeout(() => {
            hls.startLoad();
            retryCount++;
            retryDelay *= 2; // Exponential backoff
            isReconnecting = false;
        }, retryDelay);
    } else if (retryCount >= maxRetries) {
        console.error('Batas maksimum percobaan tercapai');
        showMaxRetriesAlert();
        hls.destroy();
    }
}

function handleMediaError(hls) {
    console.log('Error media, mencoba memulihkan...');
    showMediaErrorAlert();
    hls.recoverMediaError();
}

function handleFatalError(hls) {
    console.error('Error tidak dapat dipulihkan');
    showFatalErrorAlert();
    if (hls) hls.destroy();
}

function showReconnectingAlert(attempt) {
    Swal.fire({
        icon: 'warning',
        title: 'Koneksi Terputus',
        text: `Mencoba menghubungkan kembali ke kamera... (Percobaan ${attempt}/${maxRetries})`,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000
    });
}

function showMaxRetriesAlert() {
    Swal.fire({
        icon: 'error',
        title: 'Error Koneksi',
        text: 'Gagal terhubung ke kamera setelah beberapa percobaan. Silakan periksa koneksi jaringan dan kamera.',
        showConfirmButton: true
    });
}

function showMediaErrorAlert() {
    Swal.fire({
        icon: 'warning',
        title: 'Error Media',
        text: 'Mencoba memulihkan stream kamera...',
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000
    });
}

function showFatalErrorAlert() {
    Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'Gagal terhubung ke kamera. Silakan periksa koneksi dan refresh halaman.',
        showConfirmButton: true
    });
}

function showErrorAlert(title, message) {
    Swal.fire({
        icon: 'error',
        title: title,
        text: message
    });
}

// Initialize exit camera and barcode scanner
async function initializeExitCamera() {
    try {
        exitScanner = new Html5Qrcode("interactive");
        const devices = await Html5Qrcode.getCameras();

        if (devices && devices.length) {
            selectedExitCameraId = selectedExitCameraId || devices[0].id;
            await startScanning();
        } else {
            throw new Error('Tidak ada kamera yang ditemukan');
        }
    } catch (error) {
        console.error('Error inisialisasi scanner:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Gagal menginisialisasi scanner: ' + error.message
        });
    }
}

// Start barcode scanning
async function startScanning() {
    try {
        await exitScanner.start(
            selectedExitCameraId,
            {
                fps: 10,
                qrbox: { width: 250, height: 250 },
                formatsToSupport: [
                    Html5QrcodeSupportedFormats.QR_CODE,
                    Html5QrcodeSupportedFormats.CODE_128,
                    Html5QrcodeSupportedFormats.CODE_39,
                    Html5QrcodeSupportedFormats.EAN_13
                ]
            },
            handleScannedCode,
            handleScanError
        );

        document.getElementById('startScanButton').style.display = 'none';
        document.getElementById('stopScanButton').style.display = 'inline-block';

        // Show scanning status
        Swal.fire({
            icon: 'info',
            title: 'Scanner Aktif',
            text: 'Arahkan barcode ke kamera',
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000
        });
    } catch (error) {
        console.error('Error starting scanner:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Gagal memulai scanner: ' + error.message
        });
    }
}

// Handle scanned barcode
async function handleScannedCode(decodedText) {
    try {
        // Play beep sound
        const audio = new Audio('/audio/beep.mp3');
        audio.play();

        if (isAutoExitEnabled) {
            // Show loading
            const loadingAlert = Swal.fire({
                title: 'Memproses...',
                text: 'Mohon tunggu sebentar',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Process exit
            const response = await fetch('/Parking/ProcessExit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(decodedText)
            });

            const data = await response.json();
            await loadingAlert.close();

            if (data.success) {
                updateExitDetails(data);
                await Swal.fire({
                    icon: 'success',
                    title: 'Berhasil',
                    text: data.message,
                    timer: 1500,
                    showConfirmButton: false
                });

                // Restart scanner if auto process is enabled
                if (isAutoExitEnabled) {
                    await startScanning();
                }
            } else {
                throw new Error(data.message);
            }
        } else {
            // Just update the form with scanned data
            document.getElementById('vehicleNumber').value = decodedText;
            await searchVehicle(decodedText);
        }
    } catch (error) {
        console.error('Error processing exit:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: error.message || 'Gagal memproses kendaraan keluar'
        });
    }
}

// Handle scan errors
function handleScanError(error) {
    console.warn('Scan error:', error);
}

// Motion detection for entry camera
function startMotionDetection() {
    const video = document.getElementById('entryCamera');
    const canvas = document.getElementById('entryCanvas');
    const ctx = canvas.getContext('2d');
    const roiBox = document.querySelector('.roi-box');
    let lastImageData = null;
    const motionThreshold = 30;
    const motionPixels = 1000;

    function detectMotion() {
        if (!entryCameraStream) return;

        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;

        if (lastImageData) {
            let motionCount = 0;
            for (let i = 0; i < data.length; i += 4) {
                const diff = Math.abs(data[i] - lastImageData.data[i]) +
                            Math.abs(data[i+1] - lastImageData.data[i+1]) +
                            Math.abs(data[i+2] - lastImageData.data[i+2]);
                if (diff > motionThreshold) {
                    motionCount++;
                }
            }

            if (motionCount > motionPixels) {
                roiBox.classList.add('motion-detected');
                if (isAutoEntryEnabled) {
                    captureVehicle();
                }
            } else {
                roiBox.classList.remove('motion-detected');
            }
        }

        lastImageData = imageData;
        requestAnimationFrame(detectMotion);
    }

    video.addEventListener('play', () => {
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        detectMotion();
    });
}

// Capture vehicle photo
async function captureVehicle() {
    const video = document.getElementById('entryCamera');
    const canvas = document.getElementById('entryCanvas');
    const ctx = canvas.getContext('2d');

    // Draw current frame to canvas
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

    // Convert to base64
    const base64Image = canvas.toDataURL('image/jpeg');

    // Add visual feedback
    document.querySelector('.roi-box').classList.add('capture-highlight');
    setTimeout(() => {
        document.querySelector('.roi-box').classList.remove('capture-highlight');
    }, 1000);

    return base64Image;
}

// Entry form submit handler
document.getElementById('entryForm').addEventListener('submit', async function(e) {
    e.preventDefault();

    try {
        const base64Image = await captureVehicle();
        const formData = {
            vehicleNumber: document.getElementById('vehicleNumber').value,
            vehicleType: document.getElementById('vehicleType').value,
            base64Image: base64Image
        };

        const response = await fetch('/Parking/ProcessEntry', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify(formData)
        });

        const data = await response.json();

        if (data.success) {
            // Show ticket modal
            document.getElementById('ticketContent').innerHTML = data.ticketHtml;
            const ticketModal = new bootstrap.Modal(document.getElementById('ticketModal'));
            ticketModal.show();

            // Clear form
            this.reset();
        } else {
            throw new Error(data.message);
        }
    } catch (error) {
        console.error('Error processing entry:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: error.message || 'Gagal memproses kendaraan masuk'
        });
    }
});

// Print ticket handler
document.getElementById('printTicket').addEventListener('click', function() {
    window.print();
});

// Update dashboard stats
async function updateDashboardStats() {
    try {
        const response = await fetch('/Parking/GetDashboardStats');
        const data = await response.json();

        document.getElementById('activeVehicles').textContent = data.activeVehicles;
        document.getElementById('availableSlots').textContent = data.availableSlots;
        document.getElementById('todayTransactions').textContent = data.todayTransactions;
        document.getElementById('todayRevenue').textContent = `Rp ${data.todayRevenue.toLocaleString()}`;
    } catch (error) {
        console.error('Error updating dashboard stats:', error);
    }
}

// Update recent entries
function updateRecentEntries(entries) {
    recentEntriesTable.clear();
    entries.forEach(entry => {
        recentEntriesTable.row.add([
            entry.timestamp,
            entry.vehicleNumber,
            entry.vehicleType,
            `<span class="badge bg-success">Active</span>`
        ]);
    });
    recentEntriesTable.draw();
}

// Update recent exits
function updateRecentExits(exits) {
    recentExitsTable.clear();
    exits.forEach(exit => {
        recentExitsTable.row.add([
            exit.exitTime,
            exit.vehicleNumber,
            exit.duration,
            `Rp ${exit.totalAmount.toLocaleString()}`
        ]);
    });
    recentExitsTable.draw();
}

// Initialize everything when document is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeEntryCamera();
    initializeExitCamera();
    updateDashboardStats();
    
    // Update stats every minute
    setInterval(updateDashboardStats, 60000);
});