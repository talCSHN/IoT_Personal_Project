/**
   적외선 센서에 물체가 감지되면,
   시리얼 모니터에 "Detected"라는 문장을 출력
*/
int sensor1 = A0;  // 센서핀은 A0번에 연결
int sensor2 = A1;  // 센서핀은 A1번에 연결

int val1;
int val2;

void setup() {
  Serial.begin(115200);
  pinMode(sensor1, INPUT);  // 센서값을 입력으로 설정
  pinMode(sensor2, INPUT);  // 센서값을 입력으로 설정
  Serial.println("arduino starts");
}

void loop() {
  val1 = digitalRead(sensor1);  // 센서값 읽어옴
  val2 = digitalRead(sensor2);  // 센서값 읽어옴
  if (val1 == LOW) {           // IR센서는 LOW ACTIVE로 탐지 시 LOW값을 전송함
    Serial.println("A1 Detected");
    delay(300);
  }
  else if (val2 == LOW) {
    Serial.println("A2 Detected");
    delay(300);
  }
  else
    Serial.println("0");
    delay(300);
}