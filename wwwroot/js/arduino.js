// JavaScript Kontrol Arduino
// File ini menangani interaksi client-side dengan kontroler Arduino

// Elemen DOM
let statusElement;
let openGateBtn;
let closeGateBtn;
let printBtn;
let printDataInput;
let statusInterval;

// Inisialisasi saat DOM telah sepenuhnya dimuat
document.addEventListener('DOMContentLoaded', function() {
    // Inisialisasi elemen DOM
    statusElement = document.getElementById('arduino-status');
    openGateBtn = document.getElementById('open-gate-btn');
    closeGateBtn = document.getElementById('close-gate-btn');
    printBtn = document.getElementById('print-btn');
    printDataInput = document.getElementById('print-data');

    // Tambahkan event listener
    if (openGateBtn) {
        openGateBtn.addEventListener('click', openGate);
    }
    
    if (closeGateBtn) {
        closeGateBtn.addEventListener('click', closeGate);
    }
    
    if (printBtn && printDataInput) {
        printBtn.addEventListener('click', sendPrintCommand);
    }

    // Mulai polling untuk status
    getStatus();
    statusInterval = setInterval(getStatus, 5000); // Polling setiap 5 detik
});

// Bersihkan saat meninggalkan halaman
window.addEventListener('beforeunload', function() {
    if (statusInterval) {
        clearInterval(statusInterval);
    }
});

// Dapatkan status Arduino
function getStatus() {
    fetch('/api/arduino/status')
        .then(response => {
            if (response.ok) {
                return response.json();
            }
            throw new Error('Gagal mendapatkan status Arduino');
        })
        .then(data => {
            if (statusElement) {
                statusElement.textContent = data.status || 'Tidak diketahui';
                statusElement.classList.remove('text-danger', 'text-success');
                
                if (data.status && data.status.includes('OPEN')) {
                    statusElement.classList.add('text-success');
                } else {
                    statusElement.classList.add('text-primary');
                }
            }
        })
        .catch(error => {
            console.error('Error:', error);
            if (statusElement) {
                statusElement.textContent = 'Kesalahan Koneksi';
                statusElement.classList.remove('text-success', 'text-primary');
                statusElement.classList.add('text-danger');
            }
        });
}

// Buka gerbang
function openGate() {
    if (openGateBtn) {
        openGateBtn.disabled = true;
    }
    
    fetch('/api/arduino/gate/open', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Gagal membuka gerbang');
    })
    .then(data => {
        if (data.success) {
            showToast('Gerbang berhasil dibuka', 'success');
            // Update status segera
            getStatus();
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Gagal membuka gerbang', 'danger');
    })
    .finally(() => {
        if (openGateBtn) {
            openGateBtn.disabled = false;
        }
    });
}

// Tutup gerbang
function closeGate() {
    if (closeGateBtn) {
        closeGateBtn.disabled = true;
    }
    
    fetch('/api/arduino/gate/close', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Gagal menutup gerbang');
    })
    .then(data => {
        if (data.success) {
            showToast('Gerbang berhasil ditutup', 'success');
            // Update status segera
            getStatus();
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Gagal menutup gerbang', 'danger');
    })
    .finally(() => {
        if (closeGateBtn) {
            closeGateBtn.disabled = false;
        }
    });
}

// Kirim perintah cetak
function sendPrintCommand() {
    if (!printDataInput || !printDataInput.value.trim()) {
        showToast('Silakan masukkan data untuk dicetak', 'warning');
        return;
    }
    
    if (printBtn) {
        printBtn.disabled = true;
    }
    
    const printData = printDataInput.value.trim();
    
    fetch('/api/arduino/print', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ printData: printData })
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Gagal mengirim perintah cetak');
    })
    .then(data => {
        if (data.success) {
            showToast('Perintah cetak berhasil dikirim', 'success');
            if (printDataInput) {
                printDataInput.value = '';
            }
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Gagal mengirim perintah cetak', 'danger');
    })
    .finally(() => {
        if (printBtn) {
            printBtn.disabled = false;
        }
    });
}

// Tampilkan notifikasi toast
function showToast(message, type) {
    // Periksa apakah aplikasi memiliki kontainer toast
    let toastContainer = document.querySelector('.toast-container');
    
    if (!toastContainer) {
        // Buat kontainer toast jika belum ada
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // Buat elemen toast
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.id = toastId;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    // Buat konten toast
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    // Tambahkan toast ke kontainer
    toastContainer.appendChild(toast);
    
    // Inisialisasi dan tampilkan toast
    const bsToast = new bootstrap.Toast(toast, {
        autohide: true,
        delay: 3000
    });
    bsToast.show();
    
    // Hapus toast dari DOM setelah disembunyikan
    toast.addEventListener('hidden.bs.toast', function() {
        toast.remove();
    });
} 