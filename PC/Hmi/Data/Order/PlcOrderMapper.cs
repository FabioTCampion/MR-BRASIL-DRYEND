using System;
using Hmi.Data;

namespace Hmi
{
    internal static class PlcOrderMapper
    {
        public static order_TypeStruct FromPlc(dynamic src)
        {
            var dst = new order_TypeStruct();
            if (src == null) return dst;

            // Top level
            dst.startedAt = SafeGet(() => (string)src.startedAt, string.Empty);
            dst.tableID = SafeGet(() => (int)src.tableID, 0);
            dst.productionListNumber = SafeGet(() => (int)src.productionListNumber, 0);
            dst.changeOrderRequest = SafeGet(() => (bool)src.changeOrderRequest, false);
            dst.saveSQLFinished = SafeGet(() => (bool)src.saveSQLFinished, false);
            dst.saveSQLTimeOut = SafeGet(() => (bool)src.saveSQLTimeOut, false);
            dst.plcWatchDog = SafeGet(() => (bool)src.plcWatchDog, false);


            dst.paperComposition = SafeGet(() => (string)src.paperComposition, string.Empty);
            dst.fluteType = SafeGet(() => (string)src.fluteType, string.Empty);
            dst.paperWidth = SafeGet(() => (int)src.paperWidth, 0);

            dst.paper1 = SafeGet(() => (string)src.paper1, string.Empty);
            dst.paper2 = SafeGet(() => (string)src.paper2, string.Empty);
            dst.paper3 = SafeGet(() => (string)src.paper3, string.Empty);
            dst.paper4 = SafeGet(() => (string)src.paper4, string.Empty);
            dst.paper5 = SafeGet(() => (string)src.paper5, string.Empty);

            dst.lineSpeed = SafeGet(() => (float)src.lineSpeed, 0f);
            dst.linearMeters = SafeGet(() => (float)src.linearMeters, 0f);
            dst.linearMetersProduced = SafeGet(() => (float)src.linearMetersProduced, 0f);
            dst.linearMetersRemaining = SafeGet(() => (float)src.linearMetersRemaining, 0f);

            dst.scorerHeightMM = SafeGet(() => (float)src.scorerHeightMM, 0f);
            dst.levelSelector = SafeGet(() => (E_LevelSelector)(int)src.levelSelector, E_LevelSelector.Upper);
            dst.invertOrderLevel = SafeGet(() => (bool)src.invertOrderLevel, false);
            dst.invertOrderSide = SafeGet(() => (bool)src.invertOrderSide, false);

            // Child structs: order1/order2 (PLC naming)
            MapOrderVariables(SafeGetDynamic(() => src.order1, null), dst.order1);
            MapOrderVariables(SafeGetDynamic(() => src.order2, null), dst.order2);

            // generatedOrder
            MapGeneratedOrder(SafeGetDynamic(() => src.generatedOrder, null), dst.generatedOrder);

            return dst;
        }

        private static void MapOrderVariables(dynamic src, orderVariables_TypeStruct dst)
        {
            if (src == null || dst == null) return;

            dst.id = SafeGet(() => (int)src.id, 0);
            dst.product = SafeGet(() => (string)src.product, string.Empty);
            dst.client = SafeGet(() => (string)src.client, string.Empty);

            dst.sheetType = SafeGet(() => (int)src.sheetType, 0);
            dst.sheetQuantity = SafeGet(() => (int)src.sheetQuantity, 0);
            dst.sheetLength = SafeGet(() => (int)src.sheetLength, 0);

            dst.sheetM1 = SafeGet(() => (int)src.sheetM1, 0);
            dst.sheetM2 = SafeGet(() => (int)src.sheetM2, 0);
            dst.sheetM3 = SafeGet(() => (int)src.sheetM3, 0);
            dst.sheetM4 = SafeGet(() => (int)src.sheetM4, 0);
            dst.sheetM5 = SafeGet(() => (int)src.sheetM5, 0);

            dst.numberOfCuts = SafeGet(() => (int)src.numberOfCuts, 0);
            dst.numberOfCutsProduced = SafeGet(() => (int)src.numberOfCutsProduced, 0);
            dst.numberOfCutsRemaining = SafeGet(() => (int)src.numberOfCutsRemaining, 0);

            dst.pileQuantity = SafeGet(() => (int)src.pileQuantity, 0);
            dst.pileQuantityProduced = SafeGet(() => (int)src.pileQuantityProduced, 0);
            dst.pileQuantityRemaining = SafeGet(() => (int)src.pileQuantityRemaining, 0);
            dst.pileCounter = SafeGet(() => (int)src.pileCounter, 0);

            dst.scrapCounter = SafeGet(() => (int)src.scrapCounter, 0);
            dst.counterReset = SafeGet(() => (bool)src.counterReset, false);
        }

        private static void MapGeneratedOrder(dynamic src, generatedOrder_TypeStruct dst)
        {
            if (src == null || dst == null) return;

            dst.numberOfKnifes = SafeGet(() => (int)src.numberOfKnifes, 0);
            dst.numberOfScorers = SafeGet(() => (int)src.numberOfScorers, 0);
            dst.numberOfSheets = SafeGet(() => (int)src.numberOfSheets, 0);

            dst.order1Width = SafeGet(() => (float)src.order1Width, 0f);
            dst.order2Width = SafeGet(() => (float)src.order2Width, 0f);
            dst.orderTotalWidth = SafeGet(() => (float)src.orderTotalWidth, 0f);
            dst.firstKnifePosition = SafeGet(() => (float)src.firstKnifePosition, 0f);
            dst.lastKnifePosition = SafeGet(() => (float)src.lastKnifePosition, 0f);

            // PLC arrays are [1..N], C# is [0..N-1]
            for (int i = 0; i < dst.knifeEnabledArr.Length; i++)
            {
                int plcIndex = i + 1;
                dst.knifeEnabledArr[i] = SafeGet(() => (bool)src.knifeEnabledArr[plcIndex], false);
                dst.knifePositionReferenceArr[i] = SafeGet(() => (float)src.knifePositionReferenceArr[plcIndex], 0f);
            }

            for (int i = 0; i < dst.scorerEnabledArr.Length; i++)
            {
                int plcIndex = i + 1;
                dst.scorerEnabledArr[i] = SafeGet(() => (bool)src.scorerEnabledArr[plcIndex], false);
                dst.scorerPositionReferenceArr[i] = SafeGet(() => (float)src.scorerPositionReferenceArr[plcIndex], 0f);
            }
        }

        private static T SafeGet<T>(Func<T> getter, T fallback)
        {
            try { return getter(); }
            catch { return fallback; }
        }

        private static dynamic SafeGetDynamic(Func<dynamic> getter, dynamic fallback)
        {
            try { return getter(); }
            catch { return fallback; }
        }
    }
}
