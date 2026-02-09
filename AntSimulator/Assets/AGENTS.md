0) 기본 원칙

“루프(선언→거래/분석/알바→정산/지출)” 중심으로 기능을 쪼개서 만든다.

시스템끼리 직접 참조로 엉키지 않게 “이벤트(Observer)”로 통신한다.

게임_구조패턴

정적 데이터는 SO, 런타임 데이터는 Runtime 클래스 + SaveData로 분리한다.

1) Git / 작업 방식 규칙

무조건 본인 브랜치에서 작업한다. (main 직접 작업 금지)

12월_27일_회의

본인 테스트 씬만 건드린다. 다른 사람 씬/공용 씬 수정 금지.

테스트 씬 네이밍:

mkTestScene / kneTestScene / gbTestScene / dkTestScene

1월_1일_회의

Assets 루트(공용) 아래 파일은 함부로 수정하지 않는다. (추가는 가능하되, 공용 파일 변경 금지)

12월_27일_회의

2) 폴더/네임 규칙 (권장)

Scripts는 기능 단위로 분리:

Assets/Scripts/Core (GameLoop/State/공용)

Assets/Scripts/Market (시장/가격/신호)

Assets/Scripts/Player (플레이어/거래)

Assets/Scripts/UI (HUD/패널)

Assets/Scripts/Events (채널/이벤트)

Assets/Scripts/Save (SaveData/직렬화)

ScriptableObject:

Assets/Data/Definitions (StockDefinition 등)

Assets/Data/Signals (SignalTemplate)

Assets/Data/Rules (ExpenseRuleSet)

네이밍:

클래스/파일: PascalCase

메서드/필드: camelCase (상수는 UPPER_SNAKE)

ID는 문자열/정수로 고정(저장/참조용): stockId, signalId 같은 형태

데이터_구조

3) 게임 루프/상태(State) 규칙

게임 흐름은 State 패턴으로 관리한다.

인터페이스 예:

IGameState { Enter(); Tick(); Exit(); }

대표 상태 예:

MarketOpenState, SettlementState, JailState

게임_구조패턴

상태 전환은 Context(예: GameStateMachine) 가 담당한다. 상태 객체가 다른 상태를 직접 new/전환하지 않는다.

4) 데이터 설계 규칙 (SO / Runtime / Save)
   4-1. 정적 데이터 = ScriptableObject

종목 기본 스펙, 영향력자 성향, 비용 규칙, 이벤트 템플릿 등은 SO로 둔다.

예:

StockDefinition, InfluencerDefinition, SignalTemplate, ExpenseRuleSet

데이터_구조

4-2. 동적 데이터 = Runtime 클래스

플레이 중 계속 바뀌는 값:

StockState(currentPrice, candles, volume…)

InfluencerState(trustScore, lastNDays…)

PlayerState(cash, holdings, status, expenses…)

4-3. 저장(SaveData)은 “SO 저장 금지”

Save에는 SO 자체를 저장하지 말고 ID만 저장한다.

데이터_구조

예:

HoldingSave { stockId, quantity, avgBuyPrice }

StockSave { stockId, currentPrice, recentCandlesSummary }

5) 시장 계산 규칙 (입력→가중치→결과)

시장 로직은 “입력(신호) → 압력/가중치 → 가격 변화”로 일관되게 간다.

신호(선언/뉴스)는 확정 결과가 아니라 확률 가중치다.

영향력자 신뢰도(trustScore)는 신호 영향력에 곱으로 반영한다.

6) 패턴 사용 규칙 (Strategy / Observer)
   6-1. Strategy (가격 변동/판단 로직 분리)

가격 변화/변동성 로직은 Strategy로 갈아끼울 수 있게 만든다.

예:

IPriceChangeStrategy { float CalculateDelta(MarketContext ctx); }

6-2. Observer (이벤트 통신)

UI/시스템 간 연결은 이벤트로 한다. 직접 참조 금지.

게임_구조패턴

예:

선언 발생 → UI 업데이트

가격 변동 → 차트 갱신

감옥 진입 → 거래 비활성화

7) 플레이어/거래 규칙 (최소 구현 기준)

최소 기능:

cash, holdings

Buy(stockId, qty), Sell(stockId, qty) (시장가 즉시 체결부터)

감옥 상태면 거래 잠금.

8) 비용/감옥 규칙

비용 스케줄:

음식/교통: 매일

공과금: 2일마다

렌트: 3일마다

미납이면 감옥:

투자 불가

뉴스 열람만 가능

저임금 노동으로 상환 가능

9) 로그/디버깅 규칙

상태 전환/핵심 이벤트는 로그를 표준 포맷으로 남긴다.

1월_1일_회의

포맷 예:

Debug.Log($"[State] {stateName} Enter");

Debug.Log($"[Market] {stockId} delta={deltaPct:P2} price={newPrice}");

Debug.LogError($"[Transfer] ..."); (에러는 맥락이 바로 보이게)

10) 문서화 규칙 (DocumentationExample.cs 방식)

“100% 끝나고 쓰기” 말고, 50%만 돼도 문서화 시작.

1월_8일_회의

각 스크립트 문서에 포함할 것:

DocumentationExample.cs

이 스크립트의 코어 역할 1~2줄

중요한 필드(직관적이지 않은 것만 설명)

주요 함수 설명

함수가 10줄 이상이면 시그니처만 적어도 됨

helper function(단순 유틸)은 문서에서 과설명 금지

외부에서 호출하는 public API 먼저, 내부 private은 아래에 정리.

11) 클래스 관계(의존성) 규칙

가능하면 단방향 의존으로 간다. 양방향 참조는 순환참조 위험 때문에 지양.

오래 들고 쓰면 Association(필드 보유), 잠깐 쓰면 Dependency(파라미터로 받기)로 구분해서 설계한다.

기본적인_클래스간의_관계_Review!

“변경이 많은 쪽이 변경이 적은 쪽을 참조”하도록(역방향 의존 최소화).

12) 구현 우선순위 (합의된 목표)

1순위: 핵심 루프 + 주식 가격 변동

2순위: 선언 시스템 + UI

3순위: 생존비/감옥