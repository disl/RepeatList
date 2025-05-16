using Microsoft.ML;
using Microsoft.ML.Data;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
//using System.Runtime.Serialization;

namespace RepeatList.Models
{
    public class Position : BaseModel
    {
        static MLContext mlContext;
        static ITransformer? mlModel;
        static PredictionEngine<ModelInput, ModelOutput> predEngine;


        public class ModelInput
        {
            [LoadColumn(0)]
            [ColumnName(@"col0")]
            public string Col0 { get; set; }

            [LoadColumn(1)]
            [ColumnName(@"col1")]
            public string Col1 { get; set; }

        }

        public class ModelOutput
        {
            [ColumnName(@"col0")]
            public float[] Col0 { get; set; }

            [ColumnName(@"col1")]
            public uint Col1 { get; set; }

            [ColumnName(@"Features")]
            public float[] Features { get; set; }

            [ColumnName(@"PredictedLabel")]
            public string PredictedLabel { get; set; }

            [ColumnName(@"Score")]
            public float[] Score { get; set; }

        }

        public Position()
        {
            InitColors();

            if (mlContext == null)
            {
                using var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("RepeatList.Resources.ML.MLModel1.zip");
                mlContext = new MLContext();
                mlModel = mlContext.Model.Load(stream, out _);
                predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
            }
        }

        //[JsonIgnore]

        //[System.ComponentModel.DataAnnotations.Key]
        [PrimaryKey]
        public string Id { get; set; }

        [ForeignKey("HeaderId")]
        public string HeaderId { get; set; }

        private string? title;
        public string? Title
        {
            get { return title; }
            set
            {
                title = value;

                // Set Categorie
                if (string.IsNullOrEmpty(title))
                {

                }
                else
                {
                    var first_word = title.Split(' ')[0];

                    //var sampleData = new MLModel1.ModelInput()
                    //{
                    //    Col0 = first_word,
                    //};

                    ModelInput input = new ModelInput() { Col0=first_word };
                    var prediction = predEngine.Predict(input);
                    if (prediction != null)
                    {
                        Category = prediction.PredictedLabel;

                        //dataSet1.PredictionType.AddPredictionTypeRow(
                        //    s,
                        //    predicted_item.Key,
                        //    Convert.ToDecimal(predicted_item.Value));
                    }

                    //MLModel1.ModelInput sampleData = new MLModel1.ModelInput()
                    //{
                    //    Col0 = first_word,
                    //};
                    //var sortedScoresWithLabel = MLModel1.PredictAllLabels(sampleData);
                    //if (sortedScoresWithLabel != null && sortedScoresWithLabel.Count() > 0)
                    //{
                    //    var predicted_item = sortedScoresWithLabel.FirstOrDefault();

                    //    Category = predicted_item.Key;

                    //    //dataSet1.PredictionType.AddPredictionTypeRow(
                    //    //    s,
                    //    //    predicted_item.Key,
                    //    //    Convert.ToDecimal(predicted_item.Value));
                    //}
                }
            }
        }



        [JsonIgnore]
        [NotMapped]
        public string Category { get; set; } 



        //[JsonIgnore]
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        [NotMapped]
        //[IgnoreDataMember]
        public string PositionImageSource
        {
            get
            {
                string image_source;// = "check_box_outline_blank.png";
                //image_source= IsCompleted ? "check_box_outline.png" : "check_box_outline_blank.png";

                if (Application.Current.UserAppTheme == AppTheme.Dark)
                {
                    image_source= IsCompleted ? "check_box_check_white.png" : "check_box_blank_white.png";
                }
                else
                {
                    image_source= IsCompleted ? "check_box_check.png" : "check_box_blank.png";
                }
                return image_source;
            }
        }

        public bool IsCompleted { get; set; } = false;


        [JsonIgnore]
        [NotMapped]
        public Color Category_color { get; set; } = Colors.Transparent;

        [JsonIgnore]
        [NotMapped]
        private static List<Color> m_colors { get; set; } = new();



        private void InitColors()
        {
            m_colors=new List<Color>();

            if (Application.Current.UserAppTheme == AppTheme.Dark)
            {
                m_colors.Add(Colors.Yellow);
                m_colors.Add(Colors.Lime);
                m_colors.Add(Colors.Cyan);
                m_colors.Add(Colors.HotPink);
                m_colors.Add(Colors.Orange);
                m_colors.Add(Colors.Orchid);
                m_colors.Add(Colors.Gold);
                m_colors.Add(Colors.Red);
                m_colors.Add(Colors.LimeGreen);
                m_colors.Add(Colors.Turquoise);
                m_colors.Add(Colors.Magenta);
                m_colors.Add(Colors.Coral);
                m_colors.Add(Colors.SkyBlue);
                m_colors.Add(Colors.Aqua);
                m_colors.Add(Color.FromArgb("#FFEF00")); // 255, 239, 0));  // Canary Yellow
                m_colors.Add(Color.FromArgb("#0047AB"));  //0, 71, 171));      // Cobalt Blue
                m_colors.Add(Color.FromArgb("#E0115F")); // 224, 17, 95));        // Ruby Red
                m_colors.Add(Color.FromArgb("#4CBB17"));  // 76, 187, 23));     // Kelly Green
                m_colors.Add(Color.FromArgb("#9966CC")); // 153, 102, 204));     // Amethyst
                m_colors.Add(Color.FromArgb("#FF1493"));  // 255, 20, 147));      // Deep Pink
            }
            else
            {
                m_colors.Add(Color.FromArgb("#000080"));  // 0, 0, 128));    // Navy Blue
                m_colors.Add(Colors.DarkRed);
                m_colors.Add(Colors.ForestGreen);
                m_colors.Add(Colors.Indigo);
                m_colors.Add(Colors.RoyalBlue);
                m_colors.Add(Color.FromArgb("#CC5500")); // 204, 85, 0));   // Burnt Orange
                m_colors.Add(Colors.DarkMagenta);
                m_colors.Add(Color.FromArgb("#008000")); // 0, 128, 0));    // Emerald Green
                m_colors.Add(Color.FromArgb("#654321")); // 101, 67, 33));  // Chocolate Brown
                m_colors.Add(Color.FromArgb("#800020")); // 128, 0, 32));   // Burgundy
                m_colors.Add(Colors.OliveDrab);
                m_colors.Add(Colors.DarkSlateGray);
                m_colors.Add(Colors.SaddleBrown);
                m_colors.Add(Colors.MidnightBlue);
                m_colors.Add(Colors.DarkOliveGreen);
                m_colors.Add(Color.FromArgb("#0047AB"));  //0, 71, 171));      // Cobalt Blue
                m_colors.Add(Color.FromArgb("#E0115F")); // 224, 17, 95));        // Ruby Red
                m_colors.Add(Color.FromArgb("#4CBB17"));  // 76, 187, 23));     // Kelly Green
                m_colors.Add(Color.FromArgb("#9966CC")); // 153, 102, 204));     // Amethyst
                m_colors.Add(Color.FromArgb("#FF1493"));  // 255, 20, 147));      // Deep Pink
            }
        }
    }

    //internal class Categories_listType
    //{
    //    public Categories_listType(string category, Color color)
    //    {
    //        Category=category;
    //        Color=color;
    //    }

    //    public string Category { get; set; }
    //    public Color Color { get; set; }
    //}
}
