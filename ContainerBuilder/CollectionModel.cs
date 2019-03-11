using System.Collections.Generic;
using NPoco;

namespace ContainerBuilder
{
    internal enum ListType
    {
        ContainerList = 1,
        PreliminaryInventory = 2,
        Both = 3
    }

    [TableName("series")]
    [PrimaryKey("id")]
    internal class SeriesModel
    {
        public int id { get; set; }
        public int collectionID { get; set; }
        public string number { get; set; }
        public string title { get; set; }
        public bool isUnprocessed { get; set; }
        public string description { get; set; }

        [Ignore]
        public TitleDate SeriesTitleDate { get; set; }
    }

    [TableName("subseries")]
    [PrimaryKey("id")]
    internal class SubSeriesModel
    {
        public int id { get; set; }
        public int seriesID { get; set; }
        public string number { get; set; }
        public string title { get; set; }
        public int collectionID { get; set; }
        public int subseriesID { get; set; }
        public string description { get; set; }

        [Ignore]
        public TitleDate SubSeriesTitleDate { get; set; }
    }

    [TableName("subseries")]
    [PrimaryKey("id")]
    internal class SubSubSeriesModel
    {
        public int id { get; set; }
        public int seriesID { get; set; }
        public string number { get; set; }
        public string title { get; set; }
        public int collectionID { get; set; }
        public int subseriesID { get; set; }
        public string description { get; set; }

        [Ignore]
        public TitleDate SubSubSeriesTitleDate { get; set; }
    }

    [TableName("box")]
    [PrimaryKey("id")]
    internal class BoxModel
    {
        public int id { get; set; }
        public int seriesID { get; set; }
        public int subseriesID { get; set; }
        public int collectionID { get; set; }
        public string number { get; set; }
        public string title { get; set; }
        public bool isOversizeFolder { get; set; }
        public bool isUnprocessed { get; set; }
        public bool isBoxItem { get; set; }
        public string scope { get; set; }
        public string altrender { get; set; }
        public string locationCode { get; set; }

        [Ignore]
        public TitleDate BoxTitleDate { get; set; }
    }

    [TableName("folder")]
    [PrimaryKey("id")]
    internal class FolderModel
    {
        public int id { get; set; }
        public int boxID { get; set; }
        public string letter { get; set; }
        public string title { get; set; }
        public string scope { get; set; }
        public bool isOversize { get; set; }
        public int collectionID { get; set; }
        public int seriesID { get; set; }
        public int subseriesID { get; set; }
        public bool isFolderItem { get; set; }
        public bool isMapFolder { get; set; }

        [Ignore]
        public TitleDate FolderTitleDate { get; set; }
    }

    [TableName("sleeve")]
    [PrimaryKey("id")]
    internal class SleeveModel
    {
        public int id { get; set; }
        public int folderID { get; set; }
        public string number { get; set; }
        public string title { get; set; }
        public int collectionID { get; set; }

        [Ignore]
        public TitleDate SleeveTitleDate { get; set; }
    }

    [TableName("item")]
    [PrimaryKey("id")]
    internal class ItemModel
    {
        public int id { get; set; }
        public int folderID { get; set; }
        public int boxID { get; set; }
        public int seriesID { get; set; }
        public string number { get; set; }
        public string title { get; set; }
        public string scope { get; set; }
        public int collectionID { get; set; }
        public int sleeveID { get; set; }
        public bool isUnprocessed { get; set; }

        [Ignore]
        public TitleDate ItemTitleDate { get; set; }
    }

    [TableName("archival_object")]
    [PrimaryKey("id")]
    internal class ArchivalObject
    {
        public int id { get; set; }
        public int parent_id { get; set; }
    }

    [TableName("instance")]
    [PrimaryKey("id")]
    internal class Instance
    {
        public int id { get; set; }
    }

    [TableName("sub_container")]
    [PrimaryKey("id")]
    internal class SubContainer
    {
        public int id { get; set; }
    }

    [TableName("top_container_link_rlshp")]
    [PrimaryKey("id")]
    internal class TopLink
    {
        public int top_container_id { get; set; }
    }

    [TableName("top_container")]
    [PrimaryKey("id")]
    internal class TopContainer
    {
        public int id { get; set; }
    }

    [TableName("date")]
    [PrimaryKey("id")]
    internal class DateTable
    {
        public int id { get; set; }
    }

    internal class TitleDate
    {
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string type { get; set; }
        public string expression { get; set; }
    }
}
