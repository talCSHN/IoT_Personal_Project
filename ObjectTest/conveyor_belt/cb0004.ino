// 기존 핀/설정 그대로 사용
#define S0 2
#define S1 3
#define S2 8  // D4번이 패시브 부저로 사용
#define S3 5
#define OUT_PIN 6  // 센서값 확인

void setup() {
  pinMode(S0, OUTPUT); pinMode(S1, OUTPUT);
  pinMode(S2, OUTPUT); pinMode(S3, OUTPUT);
  pinMode(OUT_PIN, INPUT);

  // 20% 스케일
  digitalWrite(S0, HIGH);
  digitalWrite(S1, LOW);

  Serial.begin(115200);
}

unsigned long readColorPeriod(bool s2, bool s3) {
  digitalWrite(S2, s2); digitalWrite(S3, s3);
  delay(40);
  // 노이즈 줄이기: 7회 측정 후 중앙값
  const int N=7; unsigned long v[N];
  for (int i=0;i<N;i++){
    unsigned long p = pulseIn(OUT_PIN, LOW, 30000UL);
    v[i] = p ? p : 30000UL;
  }
  for (int i=0;i<N-1;i++){int m=i; for(int j=i+1;j<N;j++) if(v[j]<v[m]) m=j; unsigned long t=v[i]; v[i]=v[m]; v[m]=t;}
  return v[N/2];
}

void loop() {
  unsigned long pR = readColorPeriod(LOW,  LOW);   // Red
  unsigned long pB = readColorPeriod(LOW,  HIGH);  // Blue
  unsigned long pG = readColorPeriod(HIGH, HIGH);  // Green

  // 분류 규칙: 주기가 가장 작은 채널 = 지배색
  unsigned long minP = min(pR, min(pG, pB));
  unsigned long maxP = max(pR, max(pG, pB));
  unsigned long spread = maxP - minP;

  // 임계값(경험치): 필요시 조정
  bool tooDark   = (minP > 900);            // 전반적으로 어두움(검정/미접촉)
  bool nearlySame= (spread < max(15UL, minP/8)); // 세 채널이 12~15% 이내로 비슷하면 중립

  const char* name = "Unknown";
  if (tooDark) name = "Black/No object";
  else if (nearlySame) name = "White/Gray";
  else if (minP == pR) name = "Red/Orange";
  else if (minP == pG) name = "Green";
  else                 name = "Blue";

  Serial.print("Period(us)  R:"); Serial.print(pR);
  Serial.print(" G:"); Serial.print(pG);
  Serial.print(" B:"); Serial.print(pB);
  Serial.print("  | Guess: "); Serial.println(name);

  delay(150);
}