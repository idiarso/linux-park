# ParkIRC API Documentation

## Base URL
```
http://192.168.2.6:5050/api
```

## Authentication
All API endpoints require JWT authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your_token>
```

## Endpoints

### 1. Get Ticket
Retrieves ticket information by ticket number.

**URL**: `/tickets/{ticketNumber}`  
**Method**: `GET`  
**Auth Required**: Yes

**Response**:
```json
{
    "id": 1,
    "ticketNumber": "PK-123456",
    "imagePath": "/images/tickets/PK-123456.jpg",
    "timestamp": "2024-03-28T10:00:00Z",
    "status": "masuk",
    "vehicleType": "Mobil",
    "plateNumber": "B 1234 ABC",
    "exitTimestamp": null
}
```

### 2. Validate Ticket
Validates a ticket and checks if it can be used for exit.

**URL**: `/tickets/validate`  
**Method**: `POST`  
**Auth Required**: Yes

**Request Body**:
```json
{
    "ticketNumber": "PK-123456"
}
```

**Response**:
```json
{
    "isValid": true,
    "ticket": {
        "id": 1,
        "ticketNumber": "PK-123456",
        "imagePath": "/images/tickets/PK-123456.jpg",
        "timestamp": "2024-03-28T10:00:00Z",
        "status": "masuk",
        "vehicleType": "Mobil",
        "plateNumber": "B 1234 ABC",
        "exitTimestamp": null
    }
}
```

### 3. Update Exit Status
Updates a ticket's status to "keluar" and records the exit timestamp.

**URL**: `/tickets/{ticketNumber}/exit`  
**Method**: `PUT`  
**Auth Required**: Yes

**Response**:
```json
{
    "message": "Status tiket berhasil diupdate",
    "ticket": {
        "id": 1,
        "ticketNumber": "PK-123456",
        "imagePath": "/images/tickets/PK-123456.jpg",
        "timestamp": "2024-03-28T10:00:00Z",
        "status": "keluar",
        "vehicleType": "Mobil",
        "plateNumber": "B 1234 ABC",
        "exitTimestamp": "2024-03-28T12:00:00Z"
    }
}
```

## Error Responses

### 400 Bad Request
```json
{
    "message": "Nomor tiket tidak boleh kosong"
}
```

### 404 Not Found
```json
{
    "message": "Tiket tidak ditemukan"
}
```

### 500 Internal Server Error
```json
{
    "message": "Terjadi kesalahan saat memvalidasi tiket"
}
``` 