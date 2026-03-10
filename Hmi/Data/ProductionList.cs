using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dapper;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Hmi.Data
{
    public class ProductionList
    {
        public int Id { get; set; }
        public int ProductionSequence { get; set; }
        public int ProductionState { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public string PaperComposition { get; set; }
        public string FluteType { get; set; }
        public int PaperWidth { get; set; }
        public string Paper1 { get; set; }
        public string Paper2 { get; set; }
        public string Paper3 { get; set; }
        public string Paper4 { get; set; }
        public string Paper5 { get; set; }
        public int ProductionListNumber { get; set; }
        public int TopOrderId { get; set; }
        public string TopOrderProduct { get; set; }
        public string TopOrderClient { get; set; }
        public string TopOrderSheetType { get; set; }
        public int TopOrderSheetQuantity { get; set; }
        public int TopOrderM1 { get; set; }
        public int TopOrderM2 { get; set; }
        public int TopOrderM3 { get; set; }
        public int TopOrderM4 { get; set; }
        public int TopOrderM5 { get; set; }
        public int TopOrderSheetLength { get; set; }
        public int TopOrderNumberOfCuts { get; set; }
        public int TopOrderNumberOfCutsProduced { get; set; }
        public int TopOrderPileQuantity { get; set; }
        public string SecondOrderEnabled { get; set; }
        public int BottomOrderId { get; set; }
        public string BottomOrderProduct { get; set; }
        public string BottomOrderClient { get; set; }
        public string BottomOrderSheetType { get; set; }
        public int BottomOrderSheetQuantity { get; set; }
        public int BottomOrderM1 { get; set; }
        public int BottomOrderM2 { get; set; }
        public int BottomOrderM3 { get; set; }
        public int BottomOrderM4 { get; set; }
        public int BottomOrderM5 { get; set; }
        public int BottomOrderSheetLength { get; set; }
        public int BottomOrderNumberOfCuts { get; set; }
        public int BottomOrderNumberOfCutsProduced { get; set; }
        public int BottomOrderPileQuantity { get; set; }
        public string OrderDetails { get; set; }

        public string Order1Description { get; set; }
        public string Order2Description { get; set; }

    }


    public class ProductionListRepository
    {
        private readonly string connectionString;

        public ProductionListRepository()
        {
            // Assuming you have a connection string in your configuration named "cn"
            connectionString = ConfigurationManager.ConnectionStrings["cn"].ConnectionString;
        }

    }
}
