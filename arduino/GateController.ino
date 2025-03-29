/**
 * ParkIRC - Arduino Gate Controller
 * For barrier gate control and receiving print commands
 */

// Pin definitions
#define BARRIER_RELAY_PIN 4
#define IR_SENSOR_ENTRY_PIN 5
#define IR_SENSOR_EXIT_PIN 6
#define PUSH_BUTTON_PIN 7

// Vehicle detection states
bool vehicleDetected = false;
bool gateOpen = false;
unsigned long gateOpenTime = 0;
const unsigned long GATE_TIMEOUT = 10000; // 10 seconds timeout

// Communication buffer
String inputBuffer = "";
bool commandComplete = false;

void setup() {
  // Initialize serial communications
  Serial.begin(9600);
  
  // Initialize pins
  pinMode(BARRIER_RELAY_PIN, OUTPUT);
  pinMode(IR_SENSOR_ENTRY_PIN, INPUT);
  pinMode(IR_SENSOR_EXIT_PIN, INPUT);
  pinMode(PUSH_BUTTON_PIN, INPUT_PULLUP);
  
  // Set initial state
  digitalWrite(BARRIER_RELAY_PIN, LOW);
  
  // Send ready signal
  Serial.println("READY:GATE_CONTROLLER");
}

void loop() {
  // Check for commands from the host computer
  checkSerialCommands();
  
  // Check for manual button press
  checkButtonPress();
  
  // Check vehicle sensors
  checkVehicleSensors();
  
  // Check if gate should be closed (timeout)
  checkGateTimeout();
  
  // Process any completed commands
  if (commandComplete) {
    processCommand();
    commandComplete = false;
    inputBuffer = "";
  }
}

void checkSerialCommands() {
  while (Serial.available() > 0) {
    char inChar = (char)Serial.read();
    
    // End of command marker
    if (inChar == '\n') {
      commandComplete = true;
      break;
    }
    
    // Add character to buffer
    inputBuffer += inChar;
  }
}

void processCommand() {
  // Command format: CMD:PARAM1:PARAM2
  int firstColon = inputBuffer.indexOf(':');
  
  if (firstColon == -1) {
    Serial.println("ERR:INVALID_COMMAND_FORMAT");
    return;
  }
  
  String command = inputBuffer.substring(0, firstColon);
  String params = inputBuffer.substring(firstColon + 1);
  
  if (command == "OPEN") {
    openGate();
    Serial.println("OK:GATE_OPENED");
  }
  else if (command == "CLOSE") {
    closeGate();
    Serial.println("OK:GATE_CLOSED");
  }
  else if (command == "STATUS") {
    reportStatus();
  }
  else if (command == "PRINT") {
    // Just acknowledge print commands - actual printing handled by connected computer
    Serial.println("OK:PRINT_RECEIVED");
  }
  else {
    Serial.println("ERR:UNKNOWN_COMMAND");
  }
}

void checkButtonPress() {
  // Check if button is pressed (active low)
  if (digitalRead(PUSH_BUTTON_PIN) == LOW) {
    // Debounce
    delay(50);
    if (digitalRead(PUSH_BUTTON_PIN) == LOW) {
      // Button action - toggle gate
      if (gateOpen) {
        closeGate();
      } else {
        openGate();
        // Report to host computer - this will trigger ticket printing on the server
        Serial.println("EVENT:BUTTON_PRESS");
      }
      // Wait for button release
      while (digitalRead(PUSH_BUTTON_PIN) == LOW) {
        delay(10);
      }
    }
  }
}

void checkVehicleSensors() {
  // Check entry sensor
  bool vehiclePresent = (digitalRead(IR_SENSOR_ENTRY_PIN) == LOW);
  
  // Rising edge - vehicle newly detected
  if (vehiclePresent && !vehicleDetected) {
    vehicleDetected = true;
    Serial.println("EVENT:VEHICLE_DETECTED");
  }
  // Falling edge - vehicle has passed
  else if (!vehiclePresent && vehicleDetected) {
    vehicleDetected = false;
    Serial.println("EVENT:VEHICLE_PASSED");
  }
}

void checkGateTimeout() {
  // If gate is open and timeout has passed, close it
  if (gateOpen && (millis() - gateOpenTime > GATE_TIMEOUT)) {
    closeGate();
    Serial.println("EVENT:GATE_TIMEOUT");
  }
}

void openGate() {
  digitalWrite(BARRIER_RELAY_PIN, HIGH);
  gateOpen = true;
  gateOpenTime = millis();
}

void closeGate() {
  digitalWrite(BARRIER_RELAY_PIN, LOW);
  gateOpen = false;
}

void reportStatus() {
  String status = "STATUS:";
  status += gateOpen ? "OPEN:" : "CLOSED:";
  status += vehicleDetected ? "VEHICLE_PRESENT" : "NO_VEHICLE";
  
  Serial.println(status);
} 