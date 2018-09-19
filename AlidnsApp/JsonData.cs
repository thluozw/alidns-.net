namespace AlidnsApp
{
    public class IPData
    {
        /// <summary>
        /// ip
        /// </summary>
        public string ip { get; set; }
        /// <summary>
        /// 国家
        /// </summary>
        public string country { get; set; }
        /// <summary>
        /// 地区
        /// </summary>
        public string area { get; set; }
        /// <summary>
        /// 省份
        /// </summary>
        public string region { get; set; }
        /// <summary>
        /// 城市
        /// </summary>
        public string city { get; set; }
        /// <summary>
        /// 县
        /// </summary>
        public string county { get; set; }
        /// <summary>
        /// 运营商
        /// </summary>
        public string isp { get; set; }
        /// <summary>
        /// 国家ID
        /// </summary>
        public string country_id { get; set; }
        /// <summary>
        /// 地区ID
        /// </summary>
        public string area_id { get; set; }
        /// <summary>
        /// 省份ID
        /// </summary>
        public string region_id { get; set; }
        /// <summary>
        /// 城市ID
        /// </summary>
        public string city_id { get; set; }
        /// <summary>
        /// 县ID
        /// </summary>
        public string county_id { get; set; }
        /// <summary>
        /// 运营商ID
        /// </summary>
        public string isp_id { get; set; }
    }

    public class Root
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 数据
        /// </summary>
        public IPData data { get; set; }
    }
}
