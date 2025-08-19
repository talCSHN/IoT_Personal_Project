// MQTT 통신
#include <WiFiS3.h>
#include <ArduinoMqttClient.h>

// ---------- Wi-Fi 설정 ----------
const char* WIFI_SSID = "hrd301_2G";
const char* WIFI_PASS = "Pknu5234*!";

// ---------- MQTT 브로커 ----------
const char* MQTT_HOST = "210.119.12.52"; // 강사PC MQTT 브로커 주소
// 비보안(권장: 내부망/테스트) → 1883
const int   MQTT_PORT = 1883;

// TLS 사용 시 (옵션):
//  1) 아래 두 줄을 주석 해제하고
//  2) MqttClient mqttClient(sslClient); 로 바꿔 사용
//  3) 필요 시 sslClient.setCACert(rootCA_PEM); 으로 루트CA 설정
// WiFiSSLClient sslClient;   // TLS 소켓
// MqttClient mqttClient(sslClient);

// 비TLS (기본)
WiFiClient wifiClient;    
MqttClient mqttClient(wifiClient);

// (옵션) 사용자/패스워드가 필요한 브로커면 채워주세요.
const char* MQTT_USER = "";
const char* MQTT_PASSW = "";

// 발행 토픽/주기
const char* TOPIC_STATUS = "lab/unoR4/status";
unsigned long pubIntervalMs = 2000;
unsigned long lastPub = 0;

// ---------- 유틸 ----------
void connectWiFi() {
  if (WiFi.status() == WL_CONNECTED) return;

  Serial.print("WiFi connecting to ");
  Serial.println(WIFI_SSID);

  WiFi.disconnect();
  int status = WiFi.begin(WIFI_SSID, WIFI_PASS);

  unsigned long t0 = millis();
  while (WiFi.status() != WL_CONNECTED) {
    if (millis() - t0 > 15000) { // 15s 타임아웃 후 재시도
      Serial.println("WiFi retry...");
      WiFi.disconnect();
      WiFi.begin(WIFI_SSID, WIFI_PASS);
      t0 = millis();
    }
    delay(250);
    Serial.print(".");
  }
  Serial.println("\nWiFi connected!");
  Serial.print("IP: "); Serial.println(WiFi.localIP());
}

void connectMQTT() {
  if (mqttClient.connected()) return;

  // 클라이언트ID 간단 생성
  char clientId[32];
  snprintf(clientId, sizeof(clientId), "unoR4-%lu", millis());

  mqttClient.setId(clientId);
  mqttClient.setKeepAliveInterval(30); // 초
  mqttClient.setCleanSession(true);

  if (MQTT_USER && MQTT_USER[0]) {
    mqttClient.setUsernamePassword(MQTT_USER, MQTT_PASSW);
  }

  Serial.print("MQTT connecting to ");
  Serial.print(MQTT_HOST); Serial.print(":"); Serial.println(MQTT_PORT);

  while (!mqttClient.connect(MQTT_HOST, MQTT_PORT)) {
    Serial.print("MQTT connect failed, code=");
    Serial.println(mqttClient.connectError());
    delay(2000);
  }
  Serial.println("MQTT connected.");
}

// 간단 발행 헬퍼 (문자열 페이로드)
void mqttPublish(const char* topic, const char* payload) {
  mqttClient.beginMessage(topic);
  mqttClient.print(payload);
  mqttClient.endMessage();
}

// JSON 문자열(간단 조립) 발행
void publishStatus() {
  char json[160];
  long uptime = (long)(millis() / 1000);
  long rssi   = WiFi.RSSI();

  // 필요하면 여기서 센서 값들을 같이 실어보내세요
  snprintf(json, sizeof(json),
           "{\"device\":\"uno-r4\",\"uptime\":%ld,\"rssi\":%ld}", uptime, rssi);

  mqttPublish(TOPIC_STATUS, json);
  Serial.print("PUB -> "); Serial.print(TOPIC_STATUS);
  Serial.print(" : "); Serial.println(json);
}

void setup() {
  Serial.begin(115200);
  while (!Serial) {;} // (시리얼 모니터 열릴 때까지 대기 - 옵션)

  // 네트워크 시작
  connectWiFi();
  connectMQTT();
}

void loop() {
  // 네트워크/브로커 연결 유지
  if (WiFi.status() != WL_CONNECTED) connectWiFi();
  if (!mqttClient.connected()) connectMQTT();

  // MQTT keepalive 처리
  mqttClient.poll();

  // 주기 발행
  if (millis() - lastPub >= pubIntervalMs) {
    publishStatus();
    lastPub = millis();
  }
}