const entryGateMonitoring = {
    connection: null,
    
    init() {
        this.setupSignalR();
        this.setupEventListeners();
    },
    
    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/parkingHub")
            .withAutomaticReconnect()
            .build();
        
        // Monitor entry events
        this.connection.on("EntryButtonPressed", (entryPoint) => {
            this.logEvent(`Push button pressed at gate ${entryPoint}`);
            this.updatePushButtonStatus(entryPoint, true);
        });
        
        this.connection.on("TriggerCamera", (entryPoint) => {
            this.logEvent(`Camera triggered at gate ${entryPoint}`);
        });
        
        this.connection.on("PrintTicket", (ticketData) => {
            this.logEvent(`Ticket printed: ${ticketData.ticketNumber} for ${ticketData.vehicleNumber}`);
            this.showTicketPreview(ticketData);
        });
        
        this.connection.on("EntryComplete", (data) => {
            this.logEvent(`Entry complete: Ticket ${data.ticketNumber} for vehicle ${data.vehicleNumber}`);
            this.showTicketPreview(data);
        });
        
        // New event handler untuk event BarcodeGenerated
        this.connection.on("BarcodeGenerated", (data) => {
            this.logEvent(`Barcode generated: ${data.barcode} for vehicle ${data.vehicleNumber}`);
            this.showBarcodePreview(data);
            this.updateParkingHistory(data);
        });
        
        this.connection.on("OpenEntryGate", (entryPoint) => {
            this.logEvent(`Gate ${entryPoint} opened`);
            this.updateGateStatus(entryPoint, true);
            
            // After delay, close gate
            setTimeout(() => {
                this.updateGateStatus(entryPoint, false);
                this.updatePushButtonStatus(entryPoint, false);
                this.logEvent(`Gate ${entryPoint} closed automatically`);
            }, 5000);
        });
        
        this.connection.on("GateStatusChanged", (gateId, isOpen) => {
            this.updateGateStatus(gateId, isOpen);
        });
        
        this.connection.start()
            .then(() => console.log("SignalR connected for monitoring"))
            .catch(err => console.error("SignalR connection error: ", err));
    },
    
    setupEventListeners() {
        // Refresh button
        const refreshBtn = document.getElementById('refresh-gates');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => this.refreshGateStatus());
        }
        
        // Simulate push button
        const simulateBtn = document.getElementById('simulate-push-button');
        if (simulateBtn) {
            simulateBtn.addEventListener('click', () => {
                const gateSelect = document.getElementById('entry-gate-select');
                const entryPoint = gateSelect ? gateSelect.value : 'ENTRY1';
                this.simulatePushButton(entryPoint);
            });
        }
    },
    
    refreshGateStatus() {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke("GetAllGateStatus")
                .then(gates => {
                    this.updateAllGates(gates);
                })
                .catch(err => {
                    console.error("Error getting gate status: ", err);
                    this.logEvent("Error refreshing gate status", "error");
                });
        }
    },
    
    simulatePushButton(entryPoint) {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.logEvent(`Simulating push button press at gate ${entryPoint}...`);
            
            console.log('Sending PushButtonPressed event to server', {entryPoint});
            
            // Pastikan entryPoint valid
            if (!entryPoint || entryPoint === "") {
                entryPoint = "ENTRY1"; // Default fallback jika null atau kosong
            }
            
            this.connection.invoke("PushButtonPressed", entryPoint)
                .then(() => {
                    console.log('PushButtonPressed invoked successfully');
                    this.logEvent(`Push button signal sent to server`, 'success');
                })
                .catch(err => {
                    console.error("Error simulating push button: ", err);
                    this.logEvent(`Error simulating push button: ${err.message}`, "error");
                    
                    // Jika gagal, coba tanpa parameter tambahan
                    if (err.message.includes("The target does not support")) {
                        this.connection.invoke("PushButtonPressed", "ENTRY1")
                            .then(() => {
                                console.log('PushButtonPressed invoked with default parameter');
                                this.logEvent(`Retry successful with default parameter`, 'success');
                            })
                            .catch(retryErr => {
                                console.error("Retry failed: ", retryErr);
                                this.logEvent(`Retry failed: ${retryErr.message}`, "error");
                            });
                    }
                });
        } else {
            console.error('SignalR connection not established');
            this.logEvent("SignalR connection not established. Cannot simulate push button.", "error");
        }
    },
    
    updateAllGates(gates) {
        // Update gates in the table
        const gateRows = document.querySelectorAll('table tbody tr');
        gates.forEach(gate => {
            const row = Array.from(gateRows).find(row => row.cells[0].textContent === gate.name);
            if (row) {
                // Update status
                row.cells[1].innerHTML = gate.isOnline ? 
                    '<span class="badge bg-success">Online</span>' : 
                    '<span class="badge bg-danger">Offline</span>';
                
                // Update gate
                row.cells[2].innerHTML = gate.isOpen ? 
                    '<span class="badge bg-warning">Open</span>' : 
                    '<span class="badge bg-secondary">Closed</span>';
                
                // Update last activity
                row.cells[4].textContent = new Date(gate.lastActivity).toLocaleTimeString();
            }
        });
    },
    
    updateGateStatus(gateId, isOpen) {
        // Find the gate row and update it
        const gateRows = document.querySelectorAll('table tbody tr');
        const row = Array.from(gateRows).find(row => row.cells[0].textContent.includes(gateId));
        
        if (row) {
            // Update gate status
            row.cells[2].innerHTML = isOpen ? 
                '<span class="badge bg-warning">Open</span>' : 
                '<span class="badge bg-secondary">Closed</span>';
            
            // Update last activity
            row.cells[4].textContent = new Date().toLocaleTimeString();
        }
    },
    
    updatePushButtonStatus(gateId, isPressed) {
        const pushButtonStatus = document.getElementById('push-button-status');
        if (!pushButtonStatus) return;
        
        // Find if there's already a status for this gate
        const existingStatus = pushButtonStatus.querySelector(`[data-gate="${gateId}"]`);
        
        if (existingStatus) {
            // Update existing status
            existingStatus.className = `alert alert-${isPressed ? 'warning' : 'secondary'}`;
            existingStatus.innerHTML = `
                <i class="fas fa-hand-pointer me-2"></i>
                Push Button ${gateId}: ${isPressed ? 'Pressed' : 'Ready'}
            `;
        } else {
            // Create new status
            const newStatus = document.createElement('div');
            newStatus.className = `alert alert-${isPressed ? 'warning' : 'secondary'} mb-2`;
            newStatus.setAttribute('data-gate', gateId);
            newStatus.innerHTML = `
                <i class="fas fa-hand-pointer me-2"></i>
                Push Button ${gateId}: ${isPressed ? 'Pressed' : 'Ready'}
            `;
            pushButtonStatus.appendChild(newStatus);
        }
    },
    
    logEvent(message, type = 'info') {
        const simulationLog = document.getElementById('simulation-log');
        if (!simulationLog) return;
        
        const timestamp = new Date().toLocaleTimeString();
        const logEntry = document.createElement('p');
        logEntry.className = `mb-1 text-${type === 'error' ? 'danger' : type === 'warning' ? 'warning' : 'dark'}`;
        logEntry.innerHTML = `<small>[${timestamp}]</small> ${message}`;
        
        simulationLog.prepend(logEntry);
        
        // Limit log size
        if (simulationLog.children.length > 50) {
            simulationLog.removeChild(simulationLog.lastChild);
        }
    },
    
    showTicketPreview(ticketData) {
        // Create a modal for ticket preview if it doesn't exist
        let modalElement = document.getElementById('ticketPreviewModal');
        if (!modalElement) {
            modalElement = document.createElement('div');
            modalElement.className = 'modal fade';
            modalElement.id = 'ticketPreviewModal';
            modalElement.setAttribute('tabindex', '-1');
            modalElement.setAttribute('aria-labelledby', 'ticketPreviewModalLabel');
            modalElement.setAttribute('aria-hidden', 'true');
            
            document.body.appendChild(modalElement);
        }
        
        // Get site info
        const siteName = document.title.split('-')[0].trim() || 'ParkIRC System';
        const currentDate = new Date();
        const formattedDate = currentDate.toLocaleDateString('id-ID', { 
            year: 'numeric', 
            month: '2-digit', 
            day: '2-digit' 
        });
        const formattedTime = currentDate.toLocaleTimeString('id-ID', { 
            hour: '2-digit', 
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        });
        
        // Create barcode image
        const barcodeImg = `https://barcodeapi.org/api/code128/${ticketData.ticketNumber || ticketData.barcode || 'SAMPLE-00001'}`;
        
        // Fill the modal content
        modalElement.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h5 class="modal-title" id="ticketPreviewModalLabel">Print Preview - Ticket</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="ticket-preview bg-white p-3" style="font-family: monospace; width: 350px; margin: 0 auto; border: 1px dashed #ccc;">
                            <div class="text-center">
                                <p style="margin: 0; font-weight: bold;">${siteName.toUpperCase()}</p>
                                <p style="margin: 0;">PARKIR SYSTEM</p>
                                <p style="margin: 0;">-------------------------------------------</p>
                                <p style="margin: 10px 0; font-weight: bold;">TIKET PARKIR</p>
                                <p style="margin: 0; font-weight: bold;">ENTRY TICKET</p>
                            </div>
                            <div style="margin: 10px 0;">
                                <p style="margin: 0;">Nomor Tiket : ${ticketData.ticketNumber || ticketData.barcode || 'SAMPLE-00001'}</p>
                                <p style="margin: 0;">Tanggal     : ${ticketData.entryTime?.split(' ')[0] || formattedDate}</p>
                                <p style="margin: 0;">Jam Masuk   : ${ticketData.entryTime?.split(' ')[1] || formattedTime}</p>
                            </div>
                            <div class="text-center" style="margin: 10px 0;">
                                <img src="${barcodeImg}" style="max-width: 250px;" alt="Barcode">
                            </div>
                            <div class="text-center" style="margin-top: 10px;">
                                <p style="margin: 0;">-------------------------------------------</p>
                                <p style="margin: 0;">Terima Kasih</p>
                                <p style="margin: 0;">Simpan Tiket Ini</p>
                                <p style="margin: 0;">Kehilangan Tiket Dikenakan Denda</p>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        <button type="button" class="btn btn-primary" onclick="window.print()">Print</button>
                    </div>
                </div>
            </div>
        `;
        
        // Show the modal
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
        
        this.logEvent('Ticket preview displayed', 'success');
    },
    
    showBarcodePreview(data) {
        // Create a container for barcode if it doesn't exist
        let barcodeContainer = document.getElementById('barcode-container');
        if (!barcodeContainer) {
            barcodeContainer = document.createElement('div');
            barcodeContainer.id = 'barcode-container';
            barcodeContainer.className = 'card mb-3';
            
            const simulationLog = document.getElementById('simulation-log');
            if (simulationLog && simulationLog.parentNode) {
                simulationLog.parentNode.insertBefore(barcodeContainer, simulationLog);
            } else {
                // Fallback to appending to body if we can't find simulation log
                document.body.appendChild(barcodeContainer);
            }
        }
        
        // Format date and time for display
        const entryTime = new Date(data.entryTime);
        const formattedDateTime = entryTime.toLocaleString('id-ID', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        });
        
        // Build barcode content
        barcodeContainer.innerHTML = `
            <div class="card-header bg-primary text-white">
                <h5 class="mb-0">
                    <i class="fas fa-barcode me-2"></i>
                    Generated Barcode
                </h5>
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-6">
                        <div class="ticket-preview bg-white p-3" style="font-family: monospace; border: 1px dashed #ccc;">
                            <div class="text-center mb-2">
                                <strong>PARKING TICKET</strong>
                            </div>
                            <div class="text-center mb-2">
                                <img src="https://barcodeapi.org/api/code128/${data.barcode}" 
                                     class="img-fluid" 
                                     alt="Barcode: ${data.barcode}"
                                     style="max-height: 60px;">
                                <div class="mt-1 small">${data.barcode}</div>
                            </div>
                            <table class="w-100 small">
                                <tr>
                                    <td>Vehicle:</td>
                                    <td class="text-end">${data.vehicleNumber}</td>
                                </tr>
                                <tr>
                                    <td>Type:</td>
                                    <td class="text-end">${data.vehicleType}</td>
                                </tr>
                                <tr>
                                    <td>Entry:</td>
                                    <td class="text-end">${formattedDateTime}</td>
                                </tr>
                                <tr>
                                    <td>Space:</td>
                                    <td class="text-end">${data.parkingSpaceNumber || 'N/A'}</td>
                                </tr>
                            </table>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <h6>Vehicle Information</h6>
                        <table class="table table-sm table-striped">
                            <tr>
                                <th>Ticket Number:</th>
                                <td>${data.ticketNumber}</td>
                            </tr>
                            <tr>
                                <th>Vehicle Number:</th>
                                <td>${data.vehicleNumber}</td>
                            </tr>
                            <tr>
                                <th>Entry Time:</th>
                                <td>${formattedDateTime}</td>
                            </tr>
                            <tr>
                                <th>Parking Space:</th>
                                <td>${data.parkingSpaceNumber || 'N/A'}</td>
                            </tr>
                        </table>
                        <div class="text-end">
                            <button class="btn btn-sm btn-success" onclick="window.print()">
                                <i class="fas fa-print me-1"></i> Print Ticket
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },
    
    updateParkingHistory(vehicleData) {
        // Cari tabel riwayat kendaraan
        const parkingHistoryTable = document.querySelector('table.table-parking-history tbody');
        if (!parkingHistoryTable) {
            this.logEvent("Parking history table not found", "warning");
            return;
        }
        
        // Format entry time
        const entryTime = new Date(vehicleData.entryTime);
        const formattedTime = entryTime.toLocaleTimeString('id-ID', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        });
        
        // Buat row baru
        const newRow = document.createElement('tr');
        newRow.innerHTML = `
            <td>${vehicleData.ticketNumber}</td>
            <td>${vehicleData.vehicleNumber}</td>
            <td>${vehicleData.vehicleType}</td>
            <td>${formattedTime}</td>
            <td>${vehicleData.parkingSpaceNumber || 'Unknown'}</td>
            <td><span class="badge bg-success">Active</span></td>
        `;
        
        // Tambahkan row ke tabel
        parkingHistoryTable.insertBefore(newRow, parkingHistoryTable.firstChild);
        
        // Jika terlalu banyak rows, hapus yang terakhir
        if (parkingHistoryTable.children.length > 10) {
            parkingHistoryTable.removeChild(parkingHistoryTable.lastChild);
        }
        
        // Update counter jika ada
        const vehicleCounter = document.getElementById('vehicle-counter');
        if (vehicleCounter) {
            const currentCount = parseInt(vehicleCounter.textContent) || 0;
            vehicleCounter.textContent = (currentCount + 1).toString();
        }
    }
};

// Auto-initialize when document is ready
document.addEventListener('DOMContentLoaded', function() {
    entryGateMonitoring.init();
    console.log('Entry gate monitoring initialized');
}); 