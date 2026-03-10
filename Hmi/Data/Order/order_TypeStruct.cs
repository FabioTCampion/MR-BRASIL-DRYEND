using System;

namespace Hmi.Data
{
    public enum E_LevelSelector : int
    {
        Upper = 1,
        Lower = 2,
        Both = 3
    }

    public class order_TypeStruct
    {
        // DATABASE & TRACKING LOGISTICS
        public string startedAt { get; set; } = string.Empty;
        public int tableID { get; set; }
        public int productionListNumber { get; set; }
        public bool changeOrderRequest { get; set; }
        public bool saveSQLFinished { get; set; }
        public bool saveSQLTimeOut { get; set; }

        // BOARD SPECIFICATIONS
        public string paperComposition { get; set; } = string.Empty;
        public string fluteType { get; set; } = string.Empty;
        public int paperWidth { get; set; }

        // PAPER LAYERS
        public string paper1 { get; set; } = string.Empty;
        public string paper2 { get; set; } = string.Empty;
        public string paper3 { get; set; } = string.Empty;
        public string paper4 { get; set; } = string.Empty;
        public string paper5 { get; set; } = string.Empty;

        // PRODUCTION METRICS
        public float lineSpeed { get; set; }
        public float linearMeters { get; set; }
        public float linearMetersProduced { get; set; }
        public float linearMetersRemaining { get; set; }

        // TOOLING & EXECUTION CONTROL
        public float scorerHeightMM { get; set; }
        public E_LevelSelector levelSelector { get; set; } = E_LevelSelector.Upper;
        public bool invertOrderLevel { get; set; }
        public bool invertOrderSide { get; set; }

        // CHILD STRUCTURES
        public orderVariables_TypeStruct order1 { get; set; } = new orderVariables_TypeStruct();
        public orderVariables_TypeStruct order2 { get; set; } = new orderVariables_TypeStruct();
        public generatedOrder_TypeStruct generatedOrder { get; set; } = new generatedOrder_TypeStruct();
    }

    public class orderVariables_TypeStruct
    {
        // CUSTOMER & LOGISTICS DATA
        public int id { get; set; }
        public string product { get; set; } = string.Empty;
        public string client { get; set; } = string.Empty;

        // GEOMETRY & TOOLING CONFIGURATION
        public int sheetType { get; set; }
        public int sheetQuantity { get; set; }
        public int sheetLength { get; set; }

        // SCORER POSITIONS [mm]
        public int sheetM1 { get; set; }
        public int sheetM2 { get; set; }
        public int sheetM3 { get; set; }
        public int sheetM4 { get; set; }
        public int sheetM5 { get; set; }

        // PRODUCTION COUNTERS (CUTS)
        public int numberOfCuts { get; set; }
        public int numberOfCutsProduced { get; set; }
        public int numberOfCutsRemaining { get; set; }

        // STACKING & PILE MANAGEMENT
        public int pileQuantity { get; set; }
        public int pileQuantityProduced { get; set; }
        public int pileQuantityRemaining { get; set; }
        public int pileCounter { get; set; }

        // QUALITY & WASTE TRACKING
        public int scrapCounter { get; set; }
        public bool counterReset { get; set; }
    }

    public class generatedOrder_TypeStruct
    {
        // TOOLING SUMMARY
        public int numberOfKnifes { get; set; }
        public int numberOfScorers { get; set; }
        public int numberOfSheets { get; set; }

        // WEB GEOMETRY & CALCULATED WIDTHS [mm]
        public float order1Width { get; set; }
        public float order2Width { get; set; }
        public float orderTotalWidth { get; set; }
        public float firstKnifePosition { get; set; }
        public float lastKnifePosition { get; set; }

        // PLC: ARRAY [1..10]
        public bool[] knifeEnabledArr { get; set; } = new bool[10];
        public float[] knifePositionReferenceArr { get; set; } = new float[10];

        // PLC: ARRAY [1..20]
        public bool[] scorerEnabledArr { get; set; } = new bool[20];
        public float[] scorerPositionReferenceArr { get; set; } = new float[20];
    }
}
