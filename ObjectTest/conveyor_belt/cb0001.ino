// 패시브 부저 소스
int Buz = 4;  // D4 으로 부저 동작

void setup() {
  Serial.begin(9600);
  pinMode(Buz, OUTPUT);
}

void loop() {

 if (Serial.available()) {
   char input = Serial.read();

   if (input == 'h') { // Arduino IDE 시리얼 모니터에서 h를 입력, 부저 동작
     digitalWrite(Buz, HIGH);
     Serial.println("Buzzer ON");
   }
   else if (input == 'l') {  // l 입력하면 꺼짐
     digitalWrite(Buz, LOW);
     Serial.println("Buzzer OFF");
   }
 }
}