from flask import Flask, request, jsonify
from flask_cors import CORS
import psycopg2
import psycopg2.extras
from datetime import datetime

app = Flask(__name__)
CORS(app)

# Konfigurasi Database
DB_CONFIG = {
    'host': 'localhost',
    'port': '5432',
    'database': 'parkir2',
    'user': 'postgres',
    'password': 'postgres'
}

def get_db_connection():
    return psycopg2.connect(**DB_CONFIG)

# 1. API Test Koneksi
@app.route('/api/test', methods=['GET'])
def test_connection():
    try:
        conn = get_db_connection()
        cursor = conn.cursor()
        cursor.execute('SELECT COUNT(*) FROM public."Vehicles"')
        count = cursor.fetchone()[0]
        cursor.close()
        conn.close()
        return jsonify({
            'success': True,
            'message': 'Koneksi berhasil',
            'total_kendaraan': count
        })
    except Exception as e:
        return jsonify({
            'success': False,
            'message': f'Error: {str(e)}'
        }), 500

# 2. API Kendaraan Masuk
@app.route('/api/masuk', methods=['POST'])
@app.route('/api/v2/masuk', methods=['POST'])
def kendaraan_masuk():
    try:
        data = request.json
        # Support both old and new field names
        plat = data.get('plat') or data.get('plateNumber') or data.get('nomor_plat')
        jenis = data.get('jenis') or data.get('vehicleType', 'Motor')
        
        # Get isParked value, default to True if not provided
        is_parked = data.get('isParked', True)
        
        # Generate auto plate number if not provided
        if not plat:
            plat = f"UNKNOWN_{datetime.now().strftime('%y%m%d%H%M%S')}"
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
        # Generate nomor tiket offline
        cursor.execute("SELECT COUNT(*) FROM public.\"Vehicles\" WHERE \"TicketNumber\" LIKE 'OFF%'")
        count = cursor.fetchone()[0]
        ticket_number = f"OFF{str(count + 1).zfill(4)}"
        
        # Get current timestamp
        current_time = datetime.now()
        
        # Insert dengan nama kolom yang benar - hanya field yang diperlukan
        cursor.execute("""
            INSERT INTO public."Vehicles" 
            ("VehicleNumber", "VehicleType", "TicketNumber", "VehicleTypeId", 
             "EntryTime", "is_parked", "is_lost", "is_paid", "is_valid", "IsParked",
             "CreatedBy", "CreatedAt", "Status", "IsActive", "ExternalSystemId",
             "PlateNumber", "EntryGateId", "ExitGateId")
            VALUES (%s, %s, %s, %s, %s, true, false, false, true, %s,
                    'PUSH-BUTTON', %s, 'Active', true, %s,
                    %s, 'GATE1', 'GATE1')
            RETURNING "Id"
        """, (
            plat,
            jenis,
            ticket_number,
            data.get('vehicleTypeId', 2) if jenis.lower() == 'motor' else 1,
            current_time,
            is_parked,
            current_time,
            data.get('officeId', 'OFF0001'),
            plat
        ))
        
        vehicle_id = cursor.fetchone()[0]
        conn.commit()
        
        # Simplified response for push button
        response_data = {
            'success': True,
            'message': 'Kendaraan berhasil masuk',
            'data': {
                'id': vehicle_id,
                'ticket': ticket_number,
                'waktu': current_time.strftime('%Y-%m-%d %H:%M:%S')
            }
        }
        
        cursor.close()
        conn.close()
        
        return jsonify(response_data)
        
    except Exception as e:
        print(f"Error in kendaraan_masuk: {str(e)}")  # Log error
        return jsonify({
            'success': False,
            'message': f'Error: {str(e)}'
        }), 500

# 3. API Kendaraan Keluar
@app.route('/api/keluar', methods=['POST'])
def kendaraan_keluar():
    try:
        data = request.json
        tiket = data.get('tiket')
        
        if not tiket:
            return jsonify({'success': False, 'message': 'Nomor tiket wajib diisi'}), 400
        
        conn = get_db_connection()
        cursor = conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        
        # Cek kendaraan
        cursor.execute('SELECT * FROM public."Vehicles" WHERE "TicketNumber" = %s', (tiket,))
        kendaraan = cursor.fetchone()
        
        if not kendaraan:
            return jsonify({'success': False, 'message': 'Kendaraan tidak ditemukan'}), 404
        
        # Update waktu keluar
        waktu_keluar = datetime.now()
        cursor.execute("""
            UPDATE public."Vehicles" 
            SET "ExitTime" = %s 
            WHERE "TicketNumber" = %s
            RETURNING "Id", "VehicleNumber", "EntryTime"
        """, (waktu_keluar, tiket))
        
        result = cursor.fetchone()
        conn.commit()
        cursor.close()
        conn.close()
        
        return jsonify({
            'success': True,
            'message': 'Kendaraan berhasil keluar',
            'data': {
                'id': result['Id'],
                'plat': result['VehicleNumber'],
                'waktu_masuk': result['EntryTime'].isoformat(),
                'waktu_keluar': waktu_keluar.isoformat()
            }
        })
        
    except Exception as e:
        return jsonify({
            'success': False,
            'message': f'Error: {str(e)}'
        }), 500

# 4. API Daftar Kendaraan
@app.route('/api/kendaraan', methods=['GET'])
def daftar_kendaraan():
    try:
        conn = get_db_connection()
        cursor = conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        
        cursor.execute("""
            SELECT * FROM public."Vehicles" 
            ORDER BY "EntryTime" DESC 
            LIMIT 10
        """)
        
        kendaraan = cursor.fetchall()
        
        # Konversi ke format yang bisa di-JSON-kan
        hasil = []
        for k in kendaraan:
            data = dict(k)
            # Konversi datetime ke string
            for key, value in data.items():
                if isinstance(value, datetime):
                    data[key] = value.isoformat()
            hasil.append(data)
        
        cursor.close()
        conn.close()
        
        return jsonify({
            'success': True,
            'jumlah': len(hasil),
            'data': hasil
        })
        
    except Exception as e:
        return jsonify({
            'success': False,
            'message': f'Error: {str(e)}'
        }), 500

# Tambahkan endpoint untuk debugging
@app.route('/api/debug/last-request', methods=['GET'])
def debug_last_request():
    return jsonify({
        'headers': dict(request.headers),
        'method': request.method,
        'url': request.url,
        'data': request.get_data(as_text=True)
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5051, debug=True)