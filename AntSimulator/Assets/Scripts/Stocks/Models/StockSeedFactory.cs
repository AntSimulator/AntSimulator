using System.Collections.Generic;
using UnityEngine;

namespace Stocks.Models
{
    public static class StockSeedFactory
    {
        public static StockSeedDatabase BuildFromDefinitions(
            IReadOnlyList<StockDefinition> definitions,
            long currentBalance = 100000,
            int defaultAmount = 0,
            string defaultIconColor = "#FFFFFF")
        {
            // 1. 리스트 초기화 추가 (NullReference 방지)
            var db = new StockSeedDatabase
            {
                currentBalance = currentBalance,
                stocks = new List<StockSeedItem>() 
            };

            if (definitions == null || definitions.Count == 0)
            {
                return db;
            }

            // 3. 중복 방지용 Set (선택 사항)
            var addedCodes = new HashSet<string>();

            for (var i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null)
                {
                    continue;
                }

                // ID 결정 로직
                var code = string.IsNullOrWhiteSpace(def.stockId) ? def.name : def.stockId;

                // 2. 데이터 유효성 검사 (ID가 없으면 스킵)
                if (string.IsNullOrWhiteSpace(code))
                {
                    Debug.LogWarning($"[StockSeedFactory] Definition at index {i} is missing both ID and Name.");
                    continue;
                }

                // 중복 체크
                if (addedCodes.Contains(code))
                {
                    Debug.LogWarning($"[StockSeedFactory] Duplicate stock code detected: {code}");
                    continue;
                }
                addedCodes.Add(code);

                var displayName = string.IsNullOrWhiteSpace(def.displayName) ? code : def.displayName;
                
                // 가격 계산
                var price = Mathf.Max(1, Mathf.RoundToInt(def.basePrice));

                db.stocks.Add(new StockSeedItem
                {
                    code = code,
                    name = displayName,
                    iconColor = defaultIconColor,
                    icon = def.icon,
                    amount = Mathf.Max(0, defaultAmount),
                    price = price
                });
            }

            return db;
        }
    }
}