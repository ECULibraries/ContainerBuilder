using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NPoco;

namespace ContainerBuilder
{
    class Program
    {
        private static Database _db;
        private static Database _asdb;
        private static string _resourceId;
        private static string _eadId;

        private static Composite _root;
        private static int[] positions;

        protected static List<string> _updateCommands;

        static void Main(string[] args)
        {
            _db = new Database("EadConnString");
            _asdb = new Database("AsConnString");

            var dict = Utils.GetIdRelationDictionary();

            var choice = string.Empty;
           while (choice != "0")
            {
                Console.WriteLine("");
                Console.WriteLine(" 0 - Quit");
                Console.WriteLine(" 1 - Write SQL commands to create in AS");
                Console.WriteLine(" 2 - Write SQL commands to reorder in AS");
                Console.WriteLine(" 3 - Write SQL commands to delete from AS");
                Console.WriteLine(" 4 - Write + Run SQL commands to create and reorder");
                Console.WriteLine(" 5 - Write + Run SQL commands to delete");
                Console.WriteLine(" 6 - Generate Spreadsheet");
                choice = Console.ReadLine();
                if (choice == "0") return;

                Console.WriteLine("Enter EADID (0001,0002,etc...): ");
                var input = Console.ReadLine();
                if (input.Contains(","))
                {
                    var split = input.Split(',');
                    foreach (var id in split)
                    {
                        var asid = dict.FirstOrDefault(x => x.Key == id.TrimEnd('c').TrimEnd('p')).Value;
                        switch (choice)
                        {
                            case "1":
                            {
                                BuildCreates(id, asid, false);
                                break;
                            }
                            case "2":
                            {
                                BuildOrders(id, asid, false);
                                break;
                            }
                            case "3":
                            {
                                BuildDeletes(id, asid, false);
                                break;
                            }
                            case "4":
                            {
                                BuildCreates(id, asid, true);
                                BuildOrders(id, asid, true);
                                break;
                            }
                            case "5":
                            {
                                BuildDeletes(id, asid, true);
                                break;
                            }
                            case "6":
                            {
                                BuildSpreadsheet(id);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var asid = dict.FirstOrDefault(x => x.Key == input.TrimEnd('c').TrimEnd('p')).Value;
                    switch (choice)
                    {
                        case "1":
                        {
                            BuildCreates(input, asid, false);
                            break;
                        }
                        case "2":
                        {
                            BuildOrders(input, asid, false);
                            break;
                        }
                        case "3":
                        {
                            BuildDeletes(input, asid, false);
                            break;
                        }
                        case "4":
                        {
                            BuildCreates(input, asid, true);
                            BuildOrders(input, asid, true);
                            break;
                        }
                        case "5":
                        {
                            BuildDeletes(input, asid, true);
                            break;
                        }
                        case "6":
                        {
                            BuildSpreadsheet(input);
                            break;
                        }
                    }
                }
            }
        }

        private static void BuildDeletes(string eadid, string asid, bool runSql)
        {
            _eadId = eadid;
            _resourceId = asid;

            using (var deleteFile = new StreamWriter("../../SQL/" + _eadId + "_deletes.txt", false))
            {
                var ao = _asdb.Fetch<ArchivalObject>($"SELECT id FROM ars_dev.archival_object WHERE root_record_id={_resourceId} ORDER by id DESC;");
                var aoIds = string.Join(",", ao.Select(x => x.id));

                var inst = _asdb.Fetch<Instance>($"SELECT id FROM ars_dev.instance WHERE archival_object_id IN ({aoIds});");
                var instIds = string.Join(",", inst.Select(x => x.id));

                var sub = _asdb.Fetch<SubContainer>($"SELECT id FROM ars_dev.sub_container WHERE instance_id IN ({instIds});");
                var subIds = string.Join(",", sub.Select(x => x.id));

                var lnk = _asdb.Fetch<TopLink>($"SELECT top_container_id FROM ars_dev.top_container_link_rlshp WHERE sub_container_id IN ({subIds});");
                var lnkIds = string.Join(",", lnk.Select(x => x.top_container_id));

                var top = _asdb.Fetch<TopContainer>($"SELECT id FROM ars_dev.top_container WHERE id IN ({lnkIds});");
                var topIds = string.Join(",", top.Select(x => x.id));

                deleteFile.WriteLine($"DELETE FROM ars_dev.top_container_link_rlshp WHERE sub_container_id IN ({subIds});");
                deleteFile.WriteLine($"DELETE FROM ars_dev.sub_container WHERE id IN ({subIds});");
                deleteFile.WriteLine($"DELETE FROM ars_dev.instance WHERE archival_object_id IN ({aoIds});");
                deleteFile.WriteLine($"DELETE FROM ars_dev.top_container WHERE id IN ({topIds});");
                deleteFile.WriteLine($"DELETE FROM ars_dev.date WHERE archival_object_id IN ({aoIds});");
                
                foreach (var item in ao)
                {
                    deleteFile.WriteLine($"DELETE FROM ars_dev.archival_object WHERE id = {item.id};");
                }
            }

            if (runSql)
            {
                var sqlstmt = File.ReadAllText("../../SQL/" + _eadId + "_deletes.txt");
                _asdb.Execute(sqlstmt);
            }
        }

        private static void BuildOrders(string eadid, string asid, bool runSql)
        {
            _eadId = eadid;
            _resourceId = asid;

            using (var orderFile = new StreamWriter("../../SQL/" + _eadId + "_order.txt", false))
            {
                var roots = _asdb.Fetch<ArchivalObject>($"SELECT id, parent_id FROM ars_dev.archival_object WHERE root_record_id={_resourceId} AND parent_id IS NULL;");

                _updateCommands = new List<string>();
                GetUpdateCommands(roots);

                foreach (var str in _updateCommands)
                {
                    orderFile.WriteLine(str);
                }
            }

            if (runSql)
            {
                var sqlstmt = File.ReadAllText("../../SQL/" + _eadId + "_order.txt");
                _asdb.Execute(sqlstmt);
            }
        }

        private static void GetUpdateCommands(List<ArchivalObject> roots)
        {
            var pos = 500;
            foreach (var root in roots)
            {
                _updateCommands.Add($"UPDATE ars_dev.archival_object SET position = {pos} WHERE id = {root.id};");
                pos += 1000;
                var children = _asdb.Fetch<ArchivalObject>($"SELECT id,parent_id FROM ars_dev.archival_object WHERE parent_id={root.id};");
                GetUpdateCommands(children);
            }
        }

        private static void BuildSpreadsheet(string eadid)
        {
            _eadId = eadid;

            positions = new int[] { 0 };
            _root = new Composite("root");
            _root.EadId = _eadId;
            var lastBoxNumber = string.Empty;

            var started = false;
            started = StartSeries(ListType.Both);
            if (!started) started = StartBox(ListType.Both);

            _root.ReduceDepth(0);
            _root.SetPosition(positions);

            var commands = new List<string>();
            commands = _root.GetData(_eadId, commands, ref lastBoxNumber);

            var csv = new StringBuilder();
            csv.AppendLine("ID,Level,Number/Letter,Title");

            foreach (var com in commands)
            {
                csv.AppendLine(com);
            }

            File.WriteAllText("../../SQL/" + eadid + ".csv", csv.ToString(), Encoding.UTF8);
        }

        private static void BuildCreates(string eadid, string asid, bool runSql)
        {   
            var listType = eadid.EndsWith("c") ? ListType.ContainerList : eadid.EndsWith("p") ? ListType.PreliminaryInventory : ListType.Both;
            _eadId = eadid.TrimEnd('c').TrimEnd('p');
            _resourceId = asid;
            positions = new int[] { 0 };
            _root = new Composite("root");
            _root.EadId = _eadId;
            var lastBoxNumber = string.Empty;

            var started = false;
            started = StartSeries(listType);
            if (!started) started = StartBox(listType);

            _root.ReduceDepth(0);
            _root.SetPosition(positions);

            //_root.Display(1);
            //Console.ReadKey();

            var commands = new List<string>();
            commands = _root.GetCommands(_eadId, _resourceId, "", "", commands, ref lastBoxNumber);

            using (var outputFile = new StreamWriter("../../SQL/" + _eadId + ".txt", false))
            {
                foreach (var com in commands)
                {
                    outputFile.WriteLine(com);
                }
            }

            if (runSql)
            {
                var sqlstmt = File.ReadAllText("../../SQL/" + _eadId + ".txt");
                sqlstmt = sqlstmt.Replace("@", "@@");
                _asdb.Execute(sqlstmt);
            }
        }

        private static bool StartSeries(ListType listType)
        {
            var processed = listType == ListType.ContainerList
                ? " AND series.isUnprocessed IS NULL "
                : listType == ListType.PreliminaryInventory
                    ? " AND series.isUnprocessed = 1 "
                    : " ";
            var seriesList = _db.Fetch<SeriesModel>("SELECT collection.eadID, series.id, series.number, series.title, series.description " +
                "FROM series INNER JOIN collection ON collection.id = series.collectionID " +
                "WHERE collection.eadID = @0" + processed + "ORDER BY CAST(REPLACE(number, '-', '.') AS FLOAT) ASC", _eadId);
            if (!seriesList.Any()) return false;

            var depth = 1;
            var position = 1;
            foreach (var series in seriesList)
            {
                var comp = new Series("series", series);
                comp.Depth = depth;
                comp.Position = position;
                var std = Utils.GetTitleDate(series.title, series.number, "Series");
                var structuralTitle = "Series " + series.number;
                comp.isArchivalObject = std.title != structuralTitle;
                series.SeriesTitleDate = std;
                series.number = series.number.Trim();
                //var started = false;
                var started = GetSubSeriesByID("seriesID", series.id, comp);
                if (!started) started = GetBoxByID("seriesID", series.id, comp);
                //if (!started) started = GetItemByID("seriesID", series.id, comp);
                if (!started) started = GetFolderByID("seriesID", series.id, comp);
                //if (!started) started = getItemByID("seriesID", dv[i][1].ToString(), c01);

                _root.Add(comp);
                position++;
            }

            return true;
        }
        
        private static bool StartBox(ListType listType)
        {
            var processed = listType == ListType.ContainerList
                ? " AND box.isUnprocessed IS NULL "
                : listType == ListType.PreliminaryInventory
                    ? " AND box.isUnprocessed = 1 "
                    : " ";
            var boxes = _db.Fetch<BoxModel>("SELECT collection.eadID, box.id, box.number, box.title, box.isOversizeFolder, box.isBoxItem, box.scope, box.altrender, box.locationCode FROM box INNER JOIN collection ON collection.id = box.collectionID " +
                "WHERE collection.eadID = @0" + processed + "ORDER BY CASE WHEN number LIKE 'os%[a-z]' " +
                "THEN '10000' + SUBSTRING(number, 1, PATINDEX('[a-z]', number)) WHEN number LIKE '%os%' THEN '10001' + CAST(REPLACE(REPLACE(SUBSTRING(number, 1, PATINDEX('[a-z]', number)), '-', '.'), 'os', '10000') AS FLOAT) " +
                "WHEN SUBSTRING(number, 1, 1) = 'm' THEN CAST('9999' AS FLOAT) WHEN number LIKE '%[a-z]' THEN SUBSTRING(number, PATINDEX('[a-z]', number), LEN(number)) " +
                "ELSE CAST(REPLACE(REPLACE(number, '-', '.'), 'os', '.5') AS FLOAT) END ASC", _eadId);
            if (!boxes.Any()) return false;

            var depth = 1;
            var position = 1;
            foreach (var box in boxes)
            {
                var comp = new Box("box", box);
                comp.Depth = depth;
                comp.Position = position;
                var btd = Utils.GetTitleDate(box.title, box.number, "Box");
                var structuralTitle = "Box " + box.number;
                comp.isArchivalObject = btd.title != structuralTitle;

                if (box.isOversizeFolder && box.title == null)
                {
                    btd.title = "Oversize material";
                    comp.isArchivalObject = true;
                }

                box.BoxTitleDate = btd;
                box.number = box.number.Trim();

                var started = GetItemByID("boxID", box.id, comp);
                if (!started) GetFolderByID("boxID", box.id, comp);

                _root.Add(comp);
                position++;
            }

            return true;
        }

        private static bool GetSubSeriesByID(string idType, int id, Component parent)
        {
            var subseries = _db.Fetch<SubSeriesModel>("SELECT id, number, title, description FROM subseries WHERE " + idType + " = " + id);
            if (!subseries.Any()) return false;

            var depth = parent.Depth + 1;
            var position = 1;
            foreach (var sub in subseries)
            {
                var comp = new SubSeries("subseries", sub);
                comp.Depth = depth;
                comp.Position = position;
                var sstd = Utils.GetTitleDate(sub.title, sub.number, "Subseries");
                var structuralTitle = "Subseries " + sub.number;
                comp.isArchivalObject = sstd.title != structuralTitle;

                sub.SubSeriesTitleDate = sstd;
                sub.number = sub.number.Trim();

                var started = GetBoxByID("subseriesID", sub.id, comp);
                if (!started) GetFolderByID("subseriesID", sub.id, comp);
                if (!started) GetSubSubSeriesByID("subseriesID", sub.id, comp);

                parent.Add(comp);
                position++;
            }

            return true;
        }

        private static bool GetSubSubSeriesByID(string idType, int id, Component parent)
        {
            var subseries = _db.Fetch<SubSubSeriesModel>("SELECT id, number, title, description FROM subseries WHERE " + idType + " = " + id);
            if (!subseries.Any()) return false;

            var depth = parent.Depth + 1;
            var position = 1;
            foreach (var sub in subseries)
            {
                var comp = new SubSubSeries("subsubseries", sub);
                comp.Depth = depth;
                comp.Position = position;
                var sstd = Utils.GetTitleDate(sub.title, sub.number, "SubSubseries");
                var structuralTitle = "SubSubseries " + sub.number;
                comp.isArchivalObject = sstd.title != structuralTitle;

                sub.SubSubSeriesTitleDate = sstd;
                sub.number = sub.number.Trim();

                var started = GetBoxByID("subseriesID", sub.id, comp);
                if (!started) GetFolderByID("subseriesID", sub.id, comp);
                if (!started) GetSubSeriesByID("subseriesID", sub.id, comp);

                parent.Add(comp);
                position++;
            }

            return true;
        }

        private static bool GetBoxByID(string idType, int id, Component parent)
        {
            var boxes = _db.Fetch<BoxModel>("SELECT id, number, title, isOversizeFolder, scope, altrender, locationCode FROM box WHERE " + idType + " = " + id + " ORDER BY CASE WHEN number LIKE 'os%[a-z]' " +
                "THEN '10000' + SUBSTRING(number, 1, PATINDEX('[a-z]', number)) WHEN number LIKE '%os%' THEN CAST(REPLACE(REPLACE(number, '-', '.'), 'os', '10000') AS FLOAT) " +
                "WHEN SUBSTRING(number, 1, 1) = 'm' THEN CAST('9999' AS FLOAT) WHEN number LIKE '%[a-z]' THEN SUBSTRING(number, PATINDEX('[a-z]', number), LEN(number)) " +
                "ELSE CAST(REPLACE(REPLACE(number, '-', '.'), 'os', '.5') AS FLOAT) END ASC");
            if (!boxes.Any()) return false;

            var depth = parent.Depth + 1;
            var position = 1;
            foreach (var box in boxes)
            {
                var comp = new Box("box", box);
                comp.Depth = depth;
                comp.Position = position;
                var btd = Utils.GetTitleDate(box.title, box.number, "Box");
                var structuralTitle = "Box " + box.number;
                comp.isArchivalObject = btd.title != structuralTitle;

                box.BoxTitleDate = btd;
                box.number = box.number.Trim();
                var started = GetFolderByID("boxID", box.id, comp);
                if (!started) GetItemByID("boxID", box.id, comp);

                parent.Add(comp);
                position++;
            }

            return true;
        }

        private static bool GetFolderByID(string idType, int id, Component parent)
        {
            var folders = _db.Fetch<FolderModel>("SELECT id, letter, title, isOversize, isFolderItem, scope FROM folder WHERE " + idType + " = " + id +
                " ORDER BY CASE WHEN (LEN(letter) = 1) THEN '1' + letter  WHEN letter LIKE '%-%' THEN '1' + SUBSTRING(letter, 0, CHARINDEX('-', letter, 0)) " +
                "WHEN letter LIKE '[a-z].[0-9]' THEN '1' + SUBSTRING(letter, PATINDEX('[a-z]', letter), LEN(letter)) WHEN letter LIKE '%[a-z]' THEN '2' + SUBSTRING(letter, PATINDEX('[a-z]', letter), LEN(letter)) " +
                "WHEN letter LIKE '%os[0-9]' THEN REPLACE(letter, 'os', '300000') " +
                "WHEN letter LIKE '%os[0-9].[0-9]' THEN REPLACE(letter, 'os', '300000') WHEN letter LIKE '%os[0-9][0-9]' THEN REPLACE(letter, 'os', '30000') ELSE '2' + letter END ASC");
            if (!folders.Any()) return false;

            var depth = parent.Depth + 1;
            var position = 1;
            foreach (var folder in folders)
            {
                var comp = new Folder("folder", folder);
                comp.Depth = depth;
                comp.Position = position;
                var ftd = Utils.GetTitleDate(folder.title, folder.letter, "Folder");
                var structuralTitle = "Folder " + folder.letter;
                comp.isArchivalObject = ftd.title != structuralTitle;

                folder.FolderTitleDate = ftd;
                folder.letter = folder.letter.Trim();
                var started = GetSleeveByID("folderID", folder.id, comp);
                if (!started) GetItemByID("folderID", folder.id, comp);
                //if (!started) getItemPartByID("folderID", dv2[j][0].ToString(), c0x);

                parent.Add(comp);
                position++;
            }

            return true;
        }

        private static bool GetSleeveByID(string idType, int id, Component parent)
        {
            var sleeves = _db.Fetch<SleeveModel>("SELECT * FROM sleeve WHERE " + idType + " = " + id +
                                                 " ORDER BY number");
            if (!sleeves.Any()) return false;

            var depth = parent.Depth + 1;
            var position = 1;
            foreach (var sleeve in sleeves)
            {
                var comp = new Sleeve("sleeve", sleeve);
                comp.Depth = depth;
                comp.Position = position;
                var ftd = Utils.GetTitleDate(sleeve.title, sleeve.number, "Sleeve");
                var structuralTitle = "Sleeve " + sleeve.number;
                comp.isArchivalObject = ftd.title != structuralTitle;

                sleeve.SleeveTitleDate = ftd;
                sleeve.number = sleeve.number.Trim();

                GetItemByID("sleeveID", sleeve.id, comp);

                parent.Add(comp);
                position++;
            }

            return true;
        }

        private static bool GetItemByID(string idType, int id, Component parent)
        {
            var items = _db.Fetch<ItemModel>("SELECT id, number, title, pid, scope FROM item WHERE " + idType + " = " + id + " ORDER BY CASE " +
                "WHEN number LIKE '[0-9]%' THEN ABS(SUBSTRING(number, 1, PATINDEX('[space]', number))) " +
                "WHEN number LIKE '[a-z]%' THEN '1000' + LEN(number) " +
                "WHEN number LIKE 'os[0-9][a-z]' THEN 10000 " +
                "WHEN number LIKE '%[a-z]' THEN SUBSTRING(number, PATINDEX('[a-z]', number), LEN(number)) " +
                "ELSE REPLACE(number, '-', '.') END ASC");
            if (!items.Any()) return false;

            var depth = parent.Depth + 1;
            var position = 1;
            foreach (var item in items)
            {
                var comp = new Leaf("item", item);
                comp.Depth = depth;
                comp.Position = position;
                var itemTitle = string.IsNullOrEmpty(item.title) ? item.scope.Trim() : item.title.Trim();
                var itd = Utils.GetTitleDate(itemTitle, item.number, "Item");

                item.ItemTitleDate = itd;
                item.number = item.number?.Trim() ?? "NULL" + position;
                parent.Add(comp);
                position++;
            }

            return true;
        }
    }
}
