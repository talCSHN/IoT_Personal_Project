/*
   시리얼 모니터에 0~255값을 입력하여
   모터의 속도를 제어하는 응용문제
*/
int motorSpeedPin = 10;      // 1번(A) 모터 회전속도(speed)를 조절하는 핀
int motorDirectionPin = 12;  // 1번(A) 모터 방향제어(forward, backward)를 담당하는 핀
int value;                   // 모터의 속도

void setup() {
  Serial.begin(115200);                   // 시리얼 통신 시작
  noTone(4);
  pinMode(motorDirectionPin, OUTPUT);     // 방향제어핀을 pinmode, OUTPUT으로 지정
  digitalWrite(motorDirectionPin, HIGH);  // 회전 방향 지정하기. 결선 상태에 따라 전진, 후진이 결정
  value = 80;                             // 초기 속도는 80!
  analogWrite(motorSpeedPin, value);  // 초기속도 80으로 모터 작동 시작
}

void loop() {
  if (Serial.available()) {       // 시리얼 모니터에 값이 입력되었다면
    value = Serial.parseInt();  // 해당 값을 value로 지정
    if (value >= 255) {         // value값이 255보다 크다면?
      value = 255;            // 최대 값인 255로 고정
    } else if (value <= 0) {    // value값이 0보다 작다면?
      value = 0;              // 최소 값인 0으로 고정(정지)
    }

    Serial.println(value);              // value값을 시리얼모니터로 확인
    analogWrite(motorSpeedPin, value);  // 갱신된 value(=모터 속도)로 모터 제어
  }
}