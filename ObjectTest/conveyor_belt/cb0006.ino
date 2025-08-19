/**************************************************************
 * Conveyor + IR + TCS3200 + MQTT (UNO R4 WiFi)
 * - LEFT IR 감지 -> 컨베이어 시작 + MQTT Result="START"  // [NEW]
 * - RIGHT IR 도착 -> 정지 -> 1초 대기 -> 컬러 3회 측정
 * - "마지막 측정값"이 Blue면 Result="OK", Red/Orange면 "FAIL", 그 외 "FAIL"
 * - MQTT 토픽: "pknu/sf52/data"
 * - JSON: {"ClientID":"IOT01","Timestamp":"YYYY-MM-DD HH:MM:SS","Result":"..."}
 **************************************************************/

#include <WiFiS3.h>
#include <WiFiUdp.h>
#include <ArduinoMqttClient.h>

/*** ====== Wi-Fi / MQTT ====== ***/
const char* WIFI_SSID = "hrd301_2G";
const char* WIFI_PASS = "Pknu5234*!";

const char* MQTT_HOST = "210.119.12.52";
const int   MQTT_PORT = 1883;
const char* MQTT_TOPIC = "pknu/sf52/data"; // 미니프로젝트2에 사용한 토픽
const char* CLIENT_ID  = "IOT01";   // 미니프로젝트2 클라이언트아이디

WiFiClient wifiClient;
MqttClient mqttClient(wifiClient);

// 정확한 현재 시간가져오는 소스
/*** ====== NTP(Time) ====== ***/
WiFiUDP ntpUDP;
const char* NTP_SERVER = "pool.ntp.org";
const int   NTP_PORT   = 123;
const long  TZ_OFFSET  = 9 * 3600; // Asia/Seoul
unsigned long epochAtSync = 0;
unsigned long msAtSync    = 0;

bool ntpSyncOnce(unsigned long timeout_ms=2000) {
  byte packet[48]{0};
  packet[0] = 0b11100011; // LI, Version, Mode
  ntpUDP.begin(0);
  ntpUDP.beginPacket(NTP_SERVER, NTP_PORT);
  ntpUDP.write(packet, 48);
  ntpUDP.endPacket();

  unsigned long t0 = millis();
  while (millis() - t0 < timeout_ms) {
    int sz = ntpUDP.parsePacket();
    if (sz >= 48) {
      ntpUDP.read(packet, 48);
      unsigned long high = (unsigned long)packet[40] << 24 |
                           (unsigned long)packet[41] << 16 |
                           (unsigned long)packet[42] << 8  |
                           (unsigned long)packet[43];
      const unsigned long seventyYears = 2208988800UL; // 1900->1970
      unsigned long epoch = high - seventyYears;
      epochAtSync = epoch;
      msAtSync = millis();
      return true;
    }
    delay(10);
  }
  return false;
}

unsigned long currentEpoch() {
  if (epochAtSync == 0) return 0;
  return epochAtSync + (millis() - msAtSync) / 1000;
}

void formatTimestamp(char* out, size_t outsz, unsigned long epoch) {
  unsigned long t = (epoch ? epoch : 0) + TZ_OFFSET;
  unsigned long secs = t % 60; t /= 60;
  unsigned long mins = t % 60; t /= 60;
  unsigned long hours = t % 24;
  unsigned long days = t / 24;

  long z = (long)days + 719468;
  long era = (z >= 0 ? z : z - 146096) / 146097;
  unsigned doe = (unsigned)(z - era * 146097);
  unsigned yoe = (doe - doe/1460 + doe/36524 - doe/146096) / 365;
  int y = (int)yoe + (int)era * 400;
  unsigned doy = doe - (365*yoe + yoe/4 - yoe/100);
  unsigned mp = (5*doy + 2)/153;
  unsigned d = doy - (153*mp+2)/5 + 1;
  unsigned m = mp + (mp < 10 ? 3 : -9);
  y += (m <= 2);

  snprintf(out, outsz, "%04d-%02u-%02u %02lu:%02lu:%02lu",
           y, m, d, hours, mins, secs);
}

/*** ====== Pins: IR / Motor ====== ***/
const int IR_LEFT  = A0;   // 입구 IR (LOW = 감지)
const int IR_RIGHT = A1;   // 출구 IR (LOW = 감지)
const int MOTOR_PWM  = 10; // 모터 속도(PWM)
const int MOTOR_DIR  = 12; // 모터 방향

/*** ====== TCS3200 (S2=8 반영) ====== ***/
#define S0 2
#define S1 3
#define S2 8
#define S3 5
#define OUT_PIN 6

/*** ====== Params ====== ***/
const int RUN_SPEED = 90;                 // 0~255
const unsigned long SAFETY_MS = 15000UL;  // 안전 타임아웃(옵션)
const unsigned long WAIT_BEFORE_SENSE = 1000UL; // 도착 후 대기 1s
const int SENSE_COUNT = 3;                // 3회 측정

// 컬러 분류 임계치
const unsigned long DARK_US = 900;        // min 주기 > DARK_US 이면 어두움
const int SAME_PCT = 12;                  // RGB 주기가 12% 이내면 White/Gray

/*** ====== State ====== ***/
enum State { IDLE, RUNNING, COLOR_WAIT, COLOR_MEASURE } state = IDLE;
unsigned long startedAt = 0;
unsigned long senseStartAt = 0;
bool prevLeft  = false;   // ← 전역에서 관리 (중복 선언 제거)
bool prevRight = false;
int  sampleDone = 0;
int  votes[6] = {0,0,0,0,0,0}; // 0:Unknown,1:R,2:G,3:B,4:W/G,5:Black
int  lastSampleCode = 0;       // "마지막 측정값"

/*** ====== Conveyor Utils ====== ***/
void startConveyor() {
  digitalWrite(MOTOR_DIR, HIGH);  // 결선에 따라 반대로 필요하면 LOW
  analogWrite(MOTOR_PWM, RUN_SPEED);
  startedAt = millis();
  Serial.println(F("CONVEYOR: START"));
}
void stopConveyor() {
  analogWrite(MOTOR_PWM, 0);
  Serial.println(F("CONVEYOR: STOP"));
}

/*** ====== TCS3200 Utils ====== ***/
void setScale20() { digitalWrite(S0, HIGH); digitalWrite(S1, LOW); }

unsigned long readColorPeriod(bool s2, bool s3) {
  digitalWrite(S2, s2);
  digitalWrite(S3, s3);
  delay(40);
  const int N=7;
  unsigned long v[N];
  for (int i=0;i<N;i++){
    unsigned long p = pulseIn(OUT_PIN, LOW, 30000UL); // μs
    v[i] = p ? p : 30000UL;
  }
  for (int i=0;i<N-1;i++){int m=i; for(int j=i+1;j<N;j++) if(v[j]<v[m]) m=j; unsigned long t=v[i]; v[i]=v[m]; v[m]=t;}
  return v[N/2];
}

// return: 0 Unknown, 1 R/Orange, 2 G, 3 B, 4 White/Gray, 5 Black
int classifyOnce(unsigned long &pR, unsigned long &pG, unsigned long &pB) {
  pR = readColorPeriod(LOW,  LOW);   // Red
  pB = readColorPeriod(LOW,  HIGH);  // Blue
  pG = readColorPeriod(HIGH, HIGH);  // Green

  unsigned long minP = min(pR, min(pG, pB));
  unsigned long maxP = max(pR, max(pG, pB));
  unsigned long spread = maxP - minP;

  bool tooDark    = (minP > DARK_US);
  bool nearlySame = (spread < max(15UL, (minP * SAME_PCT) / 100UL));

  if (tooDark)    return 5;
  if (nearlySame) return 4;
  if (minP == pR) return 1;
  if (minP == pG) return 2;
  return 3; // Blue
}

const char* labelName(int code) {
  static const char* L[] = {
    "Unknown","Red/Orange","Green","Blue","White/Gray","Black/No object"
  };
  return L[ (code>=0 && code<=5) ? code : 0 ];
}

/*** ====== WiFi/MQTT Helpers ====== ***/
void connectWiFi() {
  if (WiFi.status() == WL_CONNECTED) return;
  Serial.print(F("WiFi connecting to ")); Serial.println(WIFI_SSID);
  WiFi.disconnect();
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  unsigned long t0 = millis();
  while (WiFi.status() != WL_CONNECTED) {
    if (millis() - t0 > 15000) { WiFi.disconnect(); WiFi.begin(WIFI_SSID, WIFI_PASS); t0 = millis(); }
    delay(250); Serial.print('.');
  }
  Serial.println(F("\nWiFi connected."));
  Serial.print(F("IP: ")); Serial.println(WiFi.localIP());
}

void connectMQTT() {
  if (mqttClient.connected()) return;
  char cid[32]; snprintf(cid, sizeof(cid), "unoR4-%lu", millis());
  mqttClient.setId(cid);
  mqttClient.setKeepAliveInterval(30);
  mqttClient.setCleanSession(true);

  Serial.print(F("MQTT connecting ")); Serial.print(MQTT_HOST); Serial.print(':'); Serial.println(MQTT_PORT);
  while (!mqttClient.connect(MQTT_HOST, MQTT_PORT)) {
    Serial.print(F("MQTT failed, code=")); Serial.println(mqttClient.connectError());
    delay(1500);
  }
  Serial.println(F("MQTT connected."));
}

void mqttPublish(const char* topic, const char* payload) {
  mqttClient.beginMessage(topic);
  mqttClient.print(payload);
  mqttClient.endMessage();
}

/*** ====== Publish Helpers ====== ***/
void publishFinalResult(int lastCode) {
  const char* result = (lastCode == 3) ? "OK" : ((lastCode == 1) ? "FAIL" : "FAIL");

  char ts[24]; ts[0] = '\0';
  unsigned long nowEpoch = currentEpoch();
  formatTimestamp(ts, sizeof(ts), nowEpoch);

  char json[160];
  snprintf(json, sizeof(json),
           "{\"ClientID\":\"%s\",\"Timestamp\":\"%s\",\"Result\":\"%s\"}",
           CLIENT_ID, ts, result);

  mqttPublish(MQTT_TOPIC, json);
  Serial.print(F("MQTT PUB -> ")); Serial.print(MQTT_TOPIC);
  Serial.print(F(" : ")); Serial.println(json);
}

// START 이벤트 발행  // [NEW]
void publishStartEvent() {
  char ts[24]; ts[0] = '\0';
  unsigned long nowEpoch = currentEpoch();
  formatTimestamp(ts, sizeof(ts), nowEpoch);

  char json[160];
  snprintf(json, sizeof(json),
           "{\"ClientID\":\"%s\",\"Timestamp\":\"%s\",\"Result\":\"START\"}",
           CLIENT_ID, ts);

  mqttPublish(MQTT_TOPIC, json);
  Serial.print(F("MQTT PUB -> ")); Serial.print(MQTT_TOPIC);
  Serial.print(F(" : ")); Serial.println(json);
}

/*** ====== SETUP ====== ***/
void setup() {
  Serial.begin(115200);
  Serial.println(F("System boot"));

  // IR
  pinMode(IR_LEFT,  INPUT);      // 필요시 INPUT_PULLUP
  pinMode(IR_RIGHT, INPUT);

  // 모터
  pinMode(MOTOR_DIR, OUTPUT);
  pinMode(MOTOR_PWM, OUTPUT);
  stopConveyor();

  // TCS3200
  pinMode(S0, OUTPUT); pinMode(S1, OUTPUT);
  pinMode(S2, OUTPUT); pinMode(S3, OUTPUT);
  pinMode(OUT_PIN, INPUT);
  setScale20(); // 20%

  // WiFi / MQTT / NTP
  connectWiFi();
  connectMQTT();
  if (ntpSyncOnce()) {
    char ts[24]; formatTimestamp(ts, sizeof(ts), currentEpoch());
    Serial.print(F("NTP synced: ")); Serial.println(ts);
  } else {
    Serial.println(F("NTP sync failed (will still publish)."));
  }
}

/*** ====== LOOP ====== ***/
void loop() {
  // 네트워크 유지
  if (WiFi.status() != WL_CONNECTED) connectWiFi();
  if (!mqttClient.connected())        connectMQTT();
  mqttClient.poll();

  // IR 읽기 (LOW active)
  bool left  = (digitalRead(IR_LEFT)  == LOW);
  bool right = (digitalRead(IR_RIGHT) == LOW);

  // 에지 검출
  bool leftEdge  = left  && !prevLeft;
  bool rightEdge = right && !prevRight;

  switch (state) {
    case IDLE:
      if (leftEdge) {
        startConveyor();
        publishStartEvent();  // ← START 메시지 발행  // [NEW]
        state = RUNNING;
      }
      break;

    case RUNNING:
      if (rightEdge) {
        stopConveyor();
        senseStartAt = millis();
        sampleDone = 0;
        lastSampleCode = 0;
        for (int i=0;i<6;i++) votes[i]=0;
        state = COLOR_WAIT;
        Serial.println(F("ARRIVED: wait 1s, then color sensing x3"));
      }
      if (SAFETY_MS && (millis() - startedAt > SAFETY_MS)) {
        Serial.println(F("TIMEOUT -> STOP"));
        stopConveyor();
        state = IDLE;
      }
      break;

    case COLOR_WAIT:
      if (millis() - senseStartAt >= WAIT_BEFORE_SENSE) {
        state = COLOR_MEASURE;
      }
      break;

    case COLOR_MEASURE: {
      if (sampleDone < SENSE_COUNT) {
        unsigned long pR,pG,pB;
        int code = classifyOnce(pR,pG,pB);
        votes[code]++;
        lastSampleCode = code; // 마지막 측정값 저장

        Serial.print(F("Sample ")); Serial.print(sampleDone+1);
        Serial.print(F("  Period(us) R:")); Serial.print(pR);
        Serial.print(F(" G:")); Serial.print(pG);
        Serial.print(F(" B:")); Serial.print(pB);
        Serial.print(F("  -> ")); Serial.println(labelName(code));

        sampleDone++;
      } else {
        // 다수결(참고 로그)
        int bestCode = 0, bestCnt = -1;
        for (int i=0;i<6;i++){
          if (votes[i] > bestCnt) { bestCnt = votes[i]; bestCode = i; }
        }
        Serial.print(F("FINAL COLOR (vote): "));
        Serial.println(labelName(bestCode));

        // 마지막 측정 결과로 MQTT 발행
        publishFinalResult(lastSampleCode);

        state = IDLE;  // 다음 물체 대기
        Serial.println(F("READY (IDLE)"));
      }
      break;
    }
  }

  prevLeft  = left;
  prevRight = right;

  delay(5);
}