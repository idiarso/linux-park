// Fungsi untuk mendapatkan token JWT
async function getAuthToken(username, password) {
    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });
        
        if (!response.ok) {
            throw new Error('Login failed');
        }
        
        const data = await response.json();
        return data.token;
    } catch (error) {
        console.error('Error getting auth token:', error);
        throw error;
    }
}

// Fungsi untuk mendapatkan data tiket
async function getTicket(ticketNumber, token) {
    try {
        const response = await fetch(`/api/tickets/${ticketNumber}`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            throw new Error('Failed to get ticket');
        }
        
        return await response.json();
    } catch (error) {
        console.error('Error getting ticket:', error);
        throw error;
    }
}

// Fungsi untuk memvalidasi tiket
async function validateTicket(ticketNumber, token) {
    try {
        const response = await fetch('/api/tickets/validate', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ ticketNumber })
        });
        
        if (!response.ok) {
            throw new Error('Failed to validate ticket');
        }
        
        return await response.json();
    } catch (error) {
        console.error('Error validating ticket:', error);
        throw error;
    }
}

// Fungsi untuk mengupdate status keluar
async function updateExitStatus(ticketNumber, token) {
    try {
        const response = await fetch(`/api/tickets/${ticketNumber}/exit`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            throw new Error('Failed to update exit status');
        }
        
        return await response.json();
    } catch (error) {
        console.error('Error updating exit status:', error);
        throw error;
    }
}

// Contoh penggunaan API
async function processTicketExit(ticketNumber) {
    try {
        // Login dan dapatkan token
        const token = await getAuthToken('operator', 'password');
        
        // Validasi tiket
        const validationResult = await validateTicket(ticketNumber, token);
        if (!validationResult.isValid) {
            console.error('Ticket is not valid');
            return;
        }
        
        // Update status keluar
        const updateResult = await updateExitStatus(ticketNumber, token);
        console.log('Exit status updated:', updateResult);
        
        // Tampilkan data tiket terakhir
        const ticket = await getTicket(ticketNumber, token);
        console.log('Final ticket status:', ticket);
        
    } catch (error) {
        console.error('Error processing ticket exit:', error);
    }
}

// Event listener untuk form validasi tiket
document.getElementById('validateForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const ticketNumber = document.getElementById('ticketNumber').value;
    await processTicketExit(ticketNumber);
}); 