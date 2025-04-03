from flask import Flask, request, jsonify, render_template
from flask_cors import CORS
import psycopg2
import psycopg2.extras
from datetime import datetime
import time

app = Flask(__name__)
CORS(app)

# Database configuration
DB_CONFIG = {
    'host': 'localhost',
    'port': '5432',
    'database': 'parkir2',
    'user': 'postgres',
    'password': 'postgres'
}

def get_db_connection():
    return psycopg2.connect(**DB_CONFIG)

# Tambahkan route untuk debugging
@app.route('/api/check-ticket/<ticket_number>')
def check_ticket(ticket_number):
    try:
        conn = get_db_connection()
        cursor = conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        
        # Cek data tiket di tabel Vehicles
        cursor.execute("""
            SELECT "Id", "TicketNumber", "VehicleNumber", "VehicleType", "EntryTime" 
            FROM public."Vehicles" 
            WHERE "TicketNumber" = %s
        """, (ticket_number,))
        
        vehicle = cursor.fetchone()
        
        if vehicle:
            return jsonify({
                'success': True,
                'data': {
                    'id': vehicle['Id'],
                    'ticketNumber': vehicle['TicketNumber'],
                    'vehicleNumber': vehicle['VehicleNumber'],
                    'vehicleType': vehicle['VehicleType'],
                    'entryTime': vehicle['EntryTime'].isoformat() if vehicle['EntryTime'] else None
                }
            })
        else:
            # Coba cari dengan format tiket yang berbeda
            cursor.execute("""
                SELECT "Id", "TicketNumber", "VehicleNumber", "VehicleType", "EntryTime" 
                FROM public."Vehicles" 
                WHERE "TicketNumber" LIKE %s
            """, (f"%{ticket_number}%",))
            
            similar_tickets = cursor.fetchall()
            return jsonify({
                'success': False,
                'message': 'Tiket tidak ditemukan',
                'similar_tickets': [
                    {
                        'ticketNumber': t['TicketNumber'],
                        'vehicleNumber': t['VehicleNumber']
                    } for t in similar_tickets
                ]
            })
        
    except Exception as e:
        return jsonify({
            'success': False,
            'message': f'Error: {str(e)}'
        }), 500
    finally:
        cursor.close()
        conn.close()

# Update template untuk menambahkan debugging info
@app.route('/exit-gate')
def exit_gate_index():
    try:
        conn = get_db_connection()
        cursor = conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        
        # Ambil contoh format tiket yang ada
        cursor.execute("""
            SELECT "TicketNumber" 
            FROM public."Vehicles" 
            ORDER BY "EntryTime" DESC 
            LIMIT 5
        """)
        
        sample_tickets = [row['TicketNumber'] for row in cursor.fetchall()]
        
        cursor.close()
        conn.close()
        
        return render_template('exit_gate.html', sample_tickets=sample_tickets)
    except Exception as e:
        return f"Error loading page: {str(e)}"

# API untuk memproses kendaraan keluar
@app.route('/api/exit-gate/process', methods=['POST'])
def process_exit():
    try:
        data = request.get_json()
        
        # Validasi data yang diterima
        if not data:
            return jsonify({
                'status': 'error',
                'message': 'No data received'
            }), 400
            
        # Proses data parkir
        # Contoh response sukses
        response = {
            'status': 'success',
            'message': 'Exit gate process successful',
            'data': {
                'ticket_id': data.get('ticket_id'),
                'exit_time': datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                'duration': '2 hours',  # Ini harus dihitung dari waktu masuk
                'fee': 5000  # Ini harus dihitung berdasarkan durasi
            }
        }
        
        return jsonify(response), 200
        
    except Exception as e:
        return jsonify({
            'status': 'error',
            'message': str(e)
        }), 500

# API untuk mendapatkan riwayat exit
@app.route('/api/exit-gate/history', methods=['GET'])
def get_exit_history():
    try:
        conn = get_db_connection()
        cursor = conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        
        cursor.execute("""
            SELECT * FROM public."VehicleExit"
            ORDER BY "ExitTime" DESC
            LIMIT 10
        """)
        
        history = []
        for row in cursor.fetchall():
            history.append({
                'id': row['Id'],
                'ticketNumber': row['TicketNumber'],
                'vehicleNumber': row['VehicleNumber'],
                'exitTime': row['ExitTime'].strftime('%Y-%m-%d %H:%M:%S'),
                'printCount': row['PrintCount']
            })
        
        cursor.close()
        conn.close()

        return jsonify({
            'success': True,
            'data': history
        })

    except Exception as e:
        return jsonify({
            'success': False,
            'message': f'Error: {str(e)}'
        }), 500

# API untuk update status cetak
@app.route('/api/exit-gate/print', methods=['POST'])
def update_print_status():
    try:
        data = request.json
        exit_id = data.get('exitId')
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            UPDATE public."VehicleExit"
            SET "PrintCount" = "PrintCount" + 1
            WHERE "Id" = %s
        """, (exit_id,))
        
        conn.commit()
        cursor.close()
        conn.close()

        return jsonify({
            'success': True,
            'message': 'Status cetak berhasil diupdate'
        })

    except Exception as e:
        return jsonify({
            'success': False,
            'message': f'Error: {str(e)}'
        }), 500

@app.route('/api/masuk', methods=['POST'])
def vehicle_entry():
    try:
        data = request.get_json() if request.is_json else {}
        
        # Get plate number from various possible field names
        plate_number = data.get('plateNumber') or data.get('plat') or data.get('nomor_plat')
        
        # Generate auto plate number jika tidak ada
        if not plate_number:
            plate_number = f"UNKNOWN_{datetime.now().strftime('%y%m%d%H%M%S')}"
        
        # Default values jika tidak ada
        vehicle_type = data.get('vehicleType') or data.get('jenis', 'Motor')
        vehicle_type_id = data.get('vehicleTypeId', 2)
        office_id = data.get('officeId', 'OFF0001')
        
        # Timestamp saat ini
        timestamp = datetime.now()
        
        # Koneksi database
        conn = get_db_connection()
        cur = conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor)
        
        # Insert ke Vehicles dengan IsParked = true
        cur.execute("""
            INSERT INTO "Vehicles" (
                "PlateNumber", 
                "VehicleType", 
                "OfficeId", 
                "VehicleTypeId",
                "EntryTime",
                "IsParked"
            ) VALUES (%s, %s, %s, %s, %s, true)
            RETURNING "Id"
        """, (
            plate_number,
            vehicle_type,
            office_id,
            vehicle_type_id,
            timestamp
        ))
        
        vehicle_id = cur.fetchone()['Id']
        
        # Generate ticket number
        ticket_number = f"TKT{timestamp.strftime('%Y%m%d%H%M%S')}"
        
        # Insert parking ticket dengan Amount = 0
        cur.execute("""
            INSERT INTO "ParkingTickets" (
                "TicketNumber",
                "BarcodeData",
                "BarcodeImagePath",
                "IssueTime",
                "EntryTime",
                "IsUsed",
                "Amount",
                "VehicleId"
            ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
            RETURNING "Id"
        """, (
            ticket_number,
            plate_number,
            f"/images/barcodes/{ticket_number}.png",
            timestamp,
            timestamp,
            False,
            0,  # Default Amount = 0
            vehicle_id
        ))
        
        conn.commit()
        
        return jsonify({
            "success": True,
            "message": "Kendaraan berhasil masuk",
            "data": {
                "ticketNumber": ticket_number,
                "plateNumber": plate_number,
                "vehicleType": vehicle_type,
                "entryTime": timestamp.strftime('%Y-%m-%d %H:%M:%S')
            }
        })
        
    except Exception as e:
        # Rollback jika ada error
        if 'conn' in locals() and conn:
            conn.rollback()
        
        return jsonify({
            "success": False,
            "message": str(e)
        }), 500
        
    finally:
        # Tutup koneksi
        if 'cur' in locals() and cur:
            cur.close()
        if 'conn' in locals() and conn:
            conn.close()

@app.route('/api/v2/masuk', methods=['POST'])
def vehicle_entry_v2():
    # Alias ke endpoint yang sama, tapi dengan versi yang jelas
    return vehicle_entry()

@app.route('/api/test', methods=['GET'])
def test():
    return jsonify({
        "success": True,
        "message": "API berfungsi dengan baik",
        "timestamp": datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5053, debug=True) 