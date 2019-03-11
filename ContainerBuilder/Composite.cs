using System;
using System.Collections.Generic;

namespace ContainerBuilder
{
    internal abstract class Component
    {
        public string name;
        public int Position;
        public bool isArchivalObject;
        public int Depth;
        public string EadId;

        protected Component(string name) { this.name = name; }
        protected string rdate = "2018-12-06";//DateTime.Now.ToString("yyyy-MM-dd");
        public abstract void Add(Component c);
        public abstract void Display(int depth);
        public abstract void ReduceDepth(int amount);
        public abstract void SetPosition(int[] positions);
        public abstract List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum);
        public abstract List<string> GetData(string refId, List<string> commands, ref string lastBoxNum);
    }

    internal class Composite : Component
    {
        protected List<Component> _children = new List<Component>();

        public Composite(string name) : base(name) { }

        public override void Add(Component component)
        {
            _children.Add(component);
        }

        public override void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + name);

            foreach (var component in _children)
            {
                component.Display(depth + 2);
            }
        }

        public override void ReduceDepth(int amount)
        {
            foreach (var component in _children)
            {
                component.ReduceDepth(amount);
            }
        }

        public override void SetPosition(int[] positions)
        {
            foreach (var component in _children)
            {
                 component.SetPosition(positions);
            }
        }

        public override List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum)
        {
            foreach (var component in _children)
            {
                component.GetCommands(refId, resourceId, "root", parentIndicator, commands, ref lastBoxNum);
            }
            return commands;
        }

        public override List<string> GetData(string refId, List<string> commands, ref string lastBoxNum)
        {
            foreach (var component in _children)
            {
                component.GetData(refId, commands, ref lastBoxNum);
            }
            return commands;
        }
    }

    internal class Series : Component
    {
        private SeriesModel _series;
        public List<Component> _children = new List<Component>();

        public Series(string name, SeriesModel series) : base(name)
        {
            _series = series;
        }

        public void Add(Component component, SeriesModel series)
        {
            _children.Add(component);
        }

        public override void Add(Component component)
        {
            _children.Add(component);
        }

        public override void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + name + Depth + ":" + Position + isArchivalObject);

            foreach (var component in _children)
            {
                component.Display(depth + 2);
            }
        }

        public override void ReduceDepth(int amount)
        {
            foreach (var component in _children)
            {
                component.ReduceDepth(amount);
            }
        }

        public override void SetPosition(int[] positions)
        {
            positions[0] += 1;
            if (positions[0] == 500 || (positions[0] - 500) % 1000 == 0) positions[0] += 1;
            Position = positions[0];

            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].SetPosition(positions);
            }

        }

        public override List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum)
        {
            refId += "-s" + _series.number;

            if (isArchivalObject)
            {
                var displayStr = string.IsNullOrEmpty(_series.SeriesTitleDate.expression) ? _series.SeriesTitleDate.title : _series.SeriesTitleDate.title + ", " + _series.SeriesTitleDate.expression;
                commands.Add("INSERT INTO `ars_dev`.`archival_object`(`lock_version`,`json_schema_version`,`repo_id`,`root_record_id`,`parent_name`,`position`,`publish`,`ref_id`,`component_id`,`title`,`display_string`,`level_id`,`system_generated`,`restrictions_apply`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`,`suppressed`)");
                commands.Add($"VALUES(0,1,3,{resourceId},'root@/repositories/3/resources/{resourceId}',{Position},1,'{refId}','{refId}','{_series.SeriesTitleDate.title}','{displayStr}'," +
                    $"895,0,0,'admin','admin','{rdate}','{rdate}','{rdate}',0);SET @seriesid = LAST_INSERT_ID();");

                if (!string.IsNullOrEmpty(_series.SeriesTitleDate.expression))
                {
                    if (_series.SeriesTitleDate.type == "901")
                    {
                        commands.Add(
                            "INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add($"VALUES(0, 1, @seriesid,{_series.SeriesTitleDate.type},906,'{_series.SeriesTitleDate.expression}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                    else
                    {
                        commands.Add(
                            "INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`begin`,`end`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add($"VALUES(0, 1, @seriesid,{_series.SeriesTitleDate.type},906,'{_series.SeriesTitleDate.expression}','{_series.SeriesTitleDate.start}','{_series.SeriesTitleDate.end}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                }
            }

            foreach (var component in _children)
            {
                component.GetCommands(refId, resourceId, "series", _series.number, commands, ref lastBoxNum);
            }

            return commands;
        }

        public override List<string> GetData(string refId, List<string> commands, ref string lastBoxNum)
        {
            commands.Add($"{_series.id},Series,{_series.number},\"{_series.title}\"");
            foreach (var component in _children)
            {
                component.GetData(refId, commands, ref lastBoxNum);
            }
            return commands;
        }
    }

    internal class SubSeries : Component
    {
        public SubSeriesModel _subseries;
        public List<Component> _children = new List<Component>();

        public SubSeries(string name, SubSeriesModel subseries) : base(name)
        {
            _subseries = subseries;
        }

        public void Add(Component component, SubSeriesModel subseries)
        {
            _children.Add(component);
        }

        public override void Add(Component component)
        {
            _children.Add(component);
        }

        public override void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + name + Depth + ":" + Position + isArchivalObject);

            foreach (var component in _children)
            {
                component.Display(depth + 2);
            }
        }

        public override void ReduceDepth(int amount)
        {
            if (!isArchivalObject)
            {
                amount++;
            }
            Depth -= amount;

            foreach (var component in _children)
            {
                component.ReduceDepth(amount);
            }
        }

        public override void SetPosition(int[] positions)
        {
            positions[0] += 1;
            if (positions[0] == 500 || (positions[0] - 500) % 1000 == 0) positions[0] += 1;
            Position = positions[0];

            foreach (var t in _children)
            {
                t.SetPosition(positions);
            }

        }

        public override List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum)
        {
            refId += "-ss" + _subseries.number;

            if (isArchivalObject)
            {
                var parentId = string.Empty;
                var parentName = string.Empty;

                if (Depth == 1)
                {
                    parentId = "NULL";
                    parentName = $"'root@/repositories/3/resources/{resourceId}'";
                }
                else
                {
                    switch (parentType)
                    {
                        case "series":
                            parentId = "@seriesid";
                            parentName = "CONCAT(@seriesid, '@archival_object')";
                            break;
                    }
                }

                //var parentId = Depth == 1 ? "NULL" : parentType == "series" ? "@seriesid" : "@subseriesid";
                //var parentName = Depth == 1 ? $"'root@/repositories/3/resources/{resourceId}'" : parentType == "series" ? "CONCAT(@seriesid, '@archival_object')" : "CONCAT(@subseriesid, '@archival_object')";

                var displayStr = string.IsNullOrEmpty(_subseries.SubSeriesTitleDate.expression) ? _subseries.SubSeriesTitleDate.title : _subseries.SubSeriesTitleDate.title + ", " + _subseries.SubSeriesTitleDate.expression;
                commands.Add("INSERT INTO `ars_dev`.`archival_object`(`lock_version`,`json_schema_version`,`repo_id`,`root_record_id`,`parent_id`,`parent_name`,`position`,`publish`,`ref_id`,`component_id`,`title`,`display_string`,`level_id`,`system_generated`,`restrictions_apply`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`,`suppressed`)");
                commands.Add($"VALUES(0,1,3,{resourceId},{parentId},{parentName},{Position},1,'{refId}','{refId}','{_subseries.SubSeriesTitleDate.title}','{displayStr}'," +
                    $"898,0,0,'admin','admin','{rdate}','{rdate}','{rdate}',0);SET @subseriesid = LAST_INSERT_ID();");

                if (!string.IsNullOrEmpty(_subseries.SubSeriesTitleDate.expression))
                {
                    if (_subseries.SubSeriesTitleDate.type == "901")
                    {
                        commands.Add(
                            "INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add(
                            $"VALUES(0, 1, @subseriesid,{_subseries.SubSeriesTitleDate.type},906,'{_subseries.SubSeriesTitleDate.expression}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                    else
                    {
                        commands.Add(
                            "INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`begin`,`end`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add(
                            $"VALUES(0, 1, @subseriesid,{_subseries.SubSeriesTitleDate.type},906,'{_subseries.SubSeriesTitleDate.expression}','{_subseries.SubSeriesTitleDate.start}','{_subseries.SubSeriesTitleDate.end}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                }
            }

            foreach (var component in _children)
            {
                if (isArchivalObject)
                    component.GetCommands(refId, resourceId, "subseries", _subseries.number, commands, ref lastBoxNum);
                else
                    component.GetCommands(refId, resourceId, parentType, parentIndicator, commands, ref lastBoxNum);
            }

            return commands;
        }

        public override List<string> GetData(string refId, List<string> commands, ref string lastBoxNum)
        {
            commands.Add($"{_subseries.id},SubSeries,{_subseries.number},\"{_subseries.title}\"");
            foreach (var component in _children)
            {
                component.GetData(refId, commands, ref lastBoxNum);
            }
            return commands;
        }
    }

    internal class SubSubSeries : Component
    {
        public SubSubSeriesModel _subseries;
        public List<Component> _children = new List<Component>();

        public SubSubSeries(string name, SubSubSeriesModel subseries) : base(name)
        {
            _subseries = subseries;
        }

        public void Add(Component component, SubSubSeriesModel subseries)
        {
            _children.Add(component);
        }

        public override void Add(Component component)
        {
            _children.Add(component);
        }

        public override void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + name + Depth + ":" + Position + isArchivalObject);

            foreach (var component in _children)
            {
                component.Display(depth + 2);
            }
        }

        public override void ReduceDepth(int amount)
        {
            if (!isArchivalObject)
            {
                amount++;
            }
            Depth -= amount;

            foreach (var component in _children)
            {
                component.ReduceDepth(amount);
            }
        }

        public override void SetPosition(int[] positions)
        {
            positions[0] += 1;
            if (positions[0] == 500 || (positions[0] - 500) % 1000 == 0) positions[0] += 1;
            Position = positions[0];

            foreach (var t in _children)
            {
                t.SetPosition(positions);
            }

        }

        public override List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum)
        {
            refId += "-sss" + _subseries.number;

            if (isArchivalObject)
            {
                var parentId = string.Empty;
                var parentName = string.Empty;

                if (Depth == 1)
                {
                    parentId = "NULL";
                    parentName = $"'root@/repositories/3/resources/{resourceId}'";
                }
                else
                {
                    switch (parentType)
                    {
                        case "subseries":
                            parentId = "@subseriesid";
                            parentName = "CONCAT(@subseriesid, '@archival_object')";
                            break;
                    }
                }

                //var parentId = Depth == 1 ? "NULL" : parentType == "series" ? "@seriesid" : "@subseriesid";
                //var parentName = Depth == 1 ? $"'root@/repositories/3/resources/{resourceId}'" : parentType == "series" ? "CONCAT(@seriesid, '@archival_object')" : "CONCAT(@subseriesid, '@archival_object')";
                //893=otherlevel
                var displayStr = string.IsNullOrEmpty(_subseries.SubSubSeriesTitleDate.expression) ? _subseries.SubSubSeriesTitleDate.title : _subseries.SubSubSeriesTitleDate.title + ", " + _subseries.SubSubSeriesTitleDate.expression;
                commands.Add("INSERT INTO `ars_dev`.`archival_object`(`lock_version`,`json_schema_version`,`repo_id`,`root_record_id`,`parent_id`,`parent_name`,`position`,`publish`,`ref_id`,`component_id`,`title`,`display_string`,`level_id`,`other_level`,`system_generated`,`restrictions_apply`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`,`suppressed`)");
                commands.Add($"VALUES(0,1,3,{resourceId},{parentId},{parentName},{Position},1,'{refId}','{refId}','{_subseries.SubSubSeriesTitleDate.title}','{displayStr}'," +
                    $"893,'Sub-Sub-Series',0,0,'admin','admin','{rdate}','{rdate}','{rdate}',0);SET @subsubseriesid = LAST_INSERT_ID();");

                if (!string.IsNullOrEmpty(_subseries.SubSubSeriesTitleDate.expression))
                {
                    if (_subseries.SubSubSeriesTitleDate.type == "901")
                    {
                        commands.Add(
                            "INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add(
                            $"VALUES(0, 1, @subsubseriesid,{_subseries.SubSubSeriesTitleDate.type},906,'{_subseries.SubSubSeriesTitleDate.expression}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                    else
                    {
                        commands.Add(
                            "INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`begin`,`end`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add(
                            $"VALUES(0, 1, @subsubseriesid,{_subseries.SubSubSeriesTitleDate.type},906,'{_subseries.SubSubSeriesTitleDate.expression}','{_subseries.SubSubSeriesTitleDate.start}','{_subseries.SubSubSeriesTitleDate.end}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                }
            }

            foreach (var component in _children)
            {
                if (isArchivalObject)
                    component.GetCommands(refId, resourceId, "subsubseries", _subseries.number, commands, ref lastBoxNum);
                else
                    component.GetCommands(refId, resourceId, parentType, parentIndicator, commands, ref lastBoxNum);
            }

            return commands;
        }

        public override List<string> GetData(string refId, List<string> commands, ref string lastBoxNum)
        {
            commands.Add($"{_subseries.id},SubSubSeries,{_subseries.number},\"{_subseries.title}\"");
            foreach (var component in _children)
            {
                component.GetData(refId, commands, ref lastBoxNum);
            }
            return commands;
        }
    }

    internal class Box : Component
    {
        public BoxModel _box;
        public List<Component> _children = new List<Component>();

        public Box(string name, BoxModel box) : base(name)
        {
            _box = box;
        }

        public void Add(Component component, BoxModel box)
        {
            _children.Add(component);
        }

        public override void Add(Component component)
        {
            _children.Add(component);
        }

        public override void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + name + Depth + ":" + Position + isArchivalObject);

            foreach (var component in _children)
            {
                component.Display(depth + 2);
            }
        }

        public override void ReduceDepth(int amount)
        {
            if (!isArchivalObject)
            {
                amount++;
            }
            Depth -= amount;

            foreach (var component in _children)
            {
                component.ReduceDepth(amount);
            }
        }

        public override void SetPosition(int[] positions)
        {
            positions[0] += 1;
            if (positions[0] == 500 || (positions[0] - 500) % 1000 == 0) positions[0] += 1;
            Position = positions[0];

            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].SetPosition(positions);
            }

        }

        public override List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum)
        {
            refId += "-b" + _box.number;

            if (isArchivalObject)
            {
                var parentId = string.Empty;
                var parentName = string.Empty;

                if (Depth == 1)
                {
                    parentId = "NULL";
                    parentName = $"'root@/repositories/3/resources/{resourceId}'";
                }
                else
                {
                    switch (parentType)
                    {
                        case "series":
                            parentId = "@seriesid";
                            parentName = "CONCAT(@seriesid, '@archival_object')";
                            break;
                        case "subseries":
                            parentId = "@subseriesid";
                            parentName = "CONCAT(@subseriesid, '@archival_object')";
                            break;
                        case "subsubseries":
                            parentId = "@subsubseriesid";
                            parentName = "CONCAT(@subsubseriesid, '@archival_object')";
                            break;
                        case "box":
                            parentId = "@boxid";
                            parentName = "CONCAT(@boxid, '@archival_object')";
                            break;
                        case "folder":
                            parentId = "@folderid";
                            parentName = "CONCAT(@folderid, '@archival_object')";
                            break;
                    }
                }
                //parentId = Depth == 1 ? "NULL" : parentType == "series" ? "@seriesid" : parentType == "subseries" ? "@subseriesid" : parentType == "box" ? "@boxid" : parentType == "folder" ? "@folderid" : "";
                //parentName = Depth == 1 ? $"'root@/repositories/3/resources/{resourceId}'" : parentType == "subseries" ? "CONCAT(@subseriesid, '@archival_object')" : parentType == "series" ? "CONCAT(@seriesid, '@archival_object')" : parentType == "box" ? "CONCAT(@boxid, '@archival_object')" : parentType == "folder" ? "CONCAT(@folderid, '@archival_object')" : "";

                var displayStr = string.IsNullOrEmpty(_box.BoxTitleDate.expression) ? _box.BoxTitleDate.title : _box.BoxTitleDate.title + ", " + _box.BoxTitleDate.expression;
                commands.Add("INSERT INTO `ars_dev`.`archival_object`(`lock_version`,`json_schema_version`,`repo_id`,`root_record_id`,`parent_id`,`parent_name`,`position`,`publish`,`ref_id`,`component_id`,`title`,`display_string`,`level_id`,`system_generated`,`restrictions_apply`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`,`suppressed`)");
                commands.Add($"VALUES(0,1,3,{resourceId},{parentId},{parentName},{Position},1,'{refId}','{refId}','{_box.BoxTitleDate.title}','{displayStr}'," +
                    $"890,0,0,'admin','admin','{rdate}','{rdate}','{rdate}',0);SET @boxid = LAST_INSERT_ID();");

                if (!string.IsNullOrEmpty(_box.BoxTitleDate.expression))
                {
                    if (_box.BoxTitleDate.type == "901")
                    {
                        commands.Add(
                            "INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add(
                            $"VALUES(0, 1, @boxid,{_box.BoxTitleDate.type},906,'{_box.BoxTitleDate.expression}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                    else
                    {
                        commands.Add(
                            "INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`begin`,`end`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add(
                            $"VALUES(0, 1, @boxid,{_box.BoxTitleDate.type},906,'{_box.BoxTitleDate.expression}','{_box.BoxTitleDate.start}','{_box.BoxTitleDate.end}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                }
                commands.Add(
                    "INSERT INTO `ars_dev`.`instance`(`lock_version`,`json_schema_version`, `archival_object_id`,`instance_type_id`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                commands.Add(
                    $"VALUES(0, 1, @boxid, 353, 'admin', 'admin', '{rdate}','{rdate}', '{rdate}'); SET @instanceid = LAST_INSERT_ID();");

                commands.Add(
                    "INSERT INTO `ars_dev`.`sub_container`(`lock_version`,`json_schema_version`,`instance_id`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                commands.Add(
                    $"VALUES(0, 1, @instanceid, 'admin','admin','{rdate}', '{rdate}', '{rdate}'); SET @subid = LAST_INSERT_ID();");
            }

            var topContainerType = "317"; //box
            if (_box.isOversizeFolder)
            {
                topContainerType = "320"; //folder;
            }

            if (lastBoxNum != _box.number)
            {
                commands.Add(
                    "INSERT INTO `ars_dev`.`top_container`(`repo_id`,`lock_version`,`json_schema_version`,`indicator`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`,`type_id`,`created_for_collection`)");
                commands.Add(
                    $"VALUES(3, 0, 1, '{_box.number}','admin','admin','{rdate}','{rdate}','{rdate}',{topContainerType},'/repositories/3/resources/{resourceId}'); SET @topid = LAST_INSERT_ID();");
            }

            lastBoxNum = _box.number;

            if (isArchivalObject)
            {
                commands.Add(
                    "INSERT INTO `ars_dev`.`top_container_link_rlshp`(`top_container_id`,`sub_container_id`,`aspace_relationship_position`,`suppressed`,`system_mtime`,`user_mtime`)");
                commands.Add($"VALUES(@topid, @subid, 0, 0,'{rdate}','{rdate}');");
            }

            foreach (var component in _children)
            {
                if (isArchivalObject)
                    component.GetCommands(refId, resourceId, "box", _box.number, commands, ref lastBoxNum);
                else
                    component.GetCommands(refId, resourceId, parentType, parentIndicator, commands, ref lastBoxNum);
            }

            return commands;
        }

        public override List<string> GetData(string refId, List<string> commands, ref string lastBoxNum)
        {
            commands.Add($"{_box.id},Box,{_box.number},\"{_box.title}\"");

            foreach (var component in _children)
            {
                component.GetData(refId, commands, ref lastBoxNum);
            }
            return commands;
        }
    }

    internal class Folder : Component
    {
        private FolderModel _folder;
        public List<Component> _children = new List<Component>();

        public Folder(string name, FolderModel folder) : base(name)
        {
            _folder = folder;
        }

        public void Add(Component component, FolderModel folder)
        {
            _folder = folder;
            _children.Add(component);
        }

        public override void Add(Component component)
        {
            _children.Add(component);
        }

        public override void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + name + Depth + ":" + Position + isArchivalObject);

            foreach (var component in _children)
            {
                component.Display(depth + 2);
            }
        }

        public override void ReduceDepth(int amount)
        {
            if (!isArchivalObject)
            {
                amount++;
            }
            Depth -= amount;
            
            foreach (var component in _children)
            {
                component.ReduceDepth(amount);
            }
        }

        public override void SetPosition(int[] positions)
        {

            positions[0] += 1;
            if (positions[0] == 500 || (positions[0] - 500) % 1000 == 0) positions[0] += 1;
            Position = positions[0];

            foreach (var component in _children)
            {
                component.SetPosition(positions);
            }
        }

        public override List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum)
        {
            refId += "-f" + _folder.letter;

            if (isArchivalObject)
            {
                var parentId = string.Empty;
                var parentName = string.Empty;

                if (Depth == 1)
                {
                    parentId = "NULL";
                    parentName = $"'root@/repositories/3/resources/{resourceId}'";
                }
                else
                {
                    switch (parentType)
                    {
                        case "series":
                            parentId = "@seriesid";
                            parentName = "CONCAT(@seriesid, '@archival_object')";
                            break;
                        case "subseries":
                            parentId = "@subseriesid";
                            parentName = "CONCAT(@subseriesid, '@archival_object')";
                            break;
                        case "subsubseries":
                            parentId = "@subsubseriesid";
                            parentName = "CONCAT(@subsubseriesid, '@archival_object')";
                            break;
                        case "box":
                            parentId = "@boxid";
                            parentName = "CONCAT(@boxid, '@archival_object')";
                            break;
                        case "folder":
                            parentId = "@folderid";
                            parentName = "CONCAT(@folderid, '@archival_object')";
                            break;
                    }
                }

                //var parentId = Depth == 1 ? "NULL" : parentType == "series" ? "@seriesid" : parentType == "subseries" ? "@subseriesid" : parentType == "box" ? "@boxid" : parentType == "folder" ? "@folderid" : "";
                //var parentName = Depth == 1 ? $"'root@/repositories/3/resources/{resourceId}'" : parentType == "subseries" ? "CONCAT(@subseriesid, '@archival_object')" : parentType == "series" ? "CONCAT(@seriesid, '@archival_object')" : parentType == "box" ? "CONCAT(@boxid, '@archival_object')" : parentType == "folder" ? "CONCAT(@folderid, '@archival_object')" : "";

                var displayStr2 = string.IsNullOrEmpty(_folder.FolderTitleDate.expression) ? _folder.FolderTitleDate.title : _folder.FolderTitleDate.title + ", " + _folder.FolderTitleDate.expression;
                commands.Add("INSERT INTO `ars_dev`.`archival_object`(`lock_version`,`json_schema_version`,`repo_id`,`root_record_id`,`parent_id`,`parent_name`,`position`,`publish`,`ref_id`,`component_id`,`title`,`display_string`,`level_id`,`system_generated`,`restrictions_apply`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`,`suppressed`)");
                commands.Add($"VALUES(0,1,3,{resourceId},{parentId},{parentName},{Position},1,'{refId}','{refId}','{_folder.FolderTitleDate.title}','{displayStr2}'," +
                    $"890,0,0,'admin','admin','{rdate}','{rdate}','{rdate}',0);SET @folderid = LAST_INSERT_ID();");

                if (!string.IsNullOrEmpty(_folder.FolderTitleDate.expression))
                {
                    if (_folder.FolderTitleDate.type == "901")
                    {
                        commands.Add("INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add($"VALUES(0, 1, @folderid,{_folder.FolderTitleDate.type},906,'{_folder.FolderTitleDate.expression}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                    else
                    {
                        commands.Add("INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`begin`,`end`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add($"VALUES(0, 1, @folderid,{_folder.FolderTitleDate.type},906,'{_folder.FolderTitleDate.expression}','{_folder.FolderTitleDate.start}','{_folder.FolderTitleDate.end}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                }
                commands.Add("INSERT INTO `ars_dev`.`instance`(`lock_version`,`json_schema_version`, `archival_object_id`,`instance_type_id`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                commands.Add($"VALUES(0, 1, @folderid, 353, 'admin','admin','{rdate}','{rdate}','{rdate}'); SET @instanceid = LAST_INSERT_ID();");

                commands.Add("INSERT INTO `ars_dev`.`sub_container`(`lock_version`,`json_schema_version`,`instance_id`,`type_2_id`,`indicator_2`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                commands.Add($"VALUES(0, 1, @instanceid, 320,'{_folder.letter}','admin','admin','{rdate}','{rdate}','{rdate}'); SET @subid = LAST_INSERT_ID();");

                commands.Add("INSERT INTO `ars_dev`.`top_container_link_rlshp`(`top_container_id`,`sub_container_id`,`aspace_relationship_position`,`suppressed`,`system_mtime`,`user_mtime`)");
                commands.Add($"VALUES(@topid, @subid, 0, 0,'{rdate}','{rdate}');");
            }

            foreach (var component in _children)
            {
                if (isArchivalObject || parentType == "root") // || component.name == "item"
                    component.GetCommands(refId, resourceId, "folder", _folder.letter, commands, ref lastBoxNum);
                else
                    component.GetCommands(refId, resourceId, parentType, _folder.letter, commands, ref lastBoxNum);
            }

            return commands;
        }

        public override List<string> GetData(string refId, List<string> commands, ref string lastBoxNum)
        {
            commands.Add($"{_folder.id},Folder,{_folder.letter},\"{_folder.title}\"");
            foreach (var component in _children)
            {
                component.GetData(refId, commands, ref lastBoxNum);
            }
            return commands;
        }
    }

    internal class Sleeve : Component
    {
        private SleeveModel _sleeve;
        public List<Component> _children = new List<Component>();

        public Sleeve(string name, SleeveModel sleeve) : base(name)
        {
            _sleeve = sleeve;
        }

        public void Add(Component component, SleeveModel sleeve)
        {
            _sleeve = sleeve;
            _children.Add(component);
        }

        public override void Add(Component component)
        {
            _children.Add(component);
        }

        public override void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + name + Depth + ":" + Position + isArchivalObject);

            foreach (var component in _children)
            {
                component.Display(depth + 2);
            }
        }

        public override void ReduceDepth(int amount)
        {
            if (!isArchivalObject)
            {
                amount++;
            }
            Depth -= amount;

            foreach (var component in _children)
            {
                component.ReduceDepth(amount);
            }
        }

        public override void SetPosition(int[] positions)
        {

            positions[0] += 1;
            if (positions[0] == 500 || (positions[0] - 500) % 1000 == 0) positions[0] += 1;
            Position = positions[0];

            foreach (var component in _children)
            {
                component.SetPosition(positions);
            }
        }

        public override List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum)
        {
            refId += "-v" + _sleeve.number;

            if (isArchivalObject)
            {
                var parentId = string.Empty;
                var parentName = string.Empty;

                if (Depth == 1)
                {
                    parentId = "NULL";
                    parentName = $"'root@/repositories/3/resources/{resourceId}'";
                }
                else
                {
                    switch (parentType)
                    {
                        case "series":
                            parentId = "@seriesid";
                            parentName = "CONCAT(@seriesid, '@archival_object')";
                            break;
                        case "subseries":
                            parentId = "@subseriesid";
                            parentName = "CONCAT(@subseriesid, '@archival_object')";
                            break;
                        case "subsubseries":
                            parentId = "@subsubseriesid";
                            parentName = "CONCAT(@subsubseriesid, '@archival_object')";
                            break;
                        case "box":
                            parentId = "@boxid";
                            parentName = "CONCAT(@boxid, '@archival_object')";
                            break;
                        case "folder":
                            parentId = "@folderid";
                            parentName = "CONCAT(@folderid, '@archival_object')";
                            break;
                    }
                }

                var displayStr2 = string.IsNullOrEmpty(_sleeve.SleeveTitleDate.expression) ? _sleeve.SleeveTitleDate.title : _sleeve.SleeveTitleDate.title + ", " + _sleeve.SleeveTitleDate.expression;
                commands.Add("INSERT INTO `ars_dev`.`archival_object`(`lock_version`,`json_schema_version`,`repo_id`,`root_record_id`,`parent_id`,`parent_name`,`position`,`publish`,`ref_id`,`component_id`,`title`,`display_string`,`level_id`,`other_level`,`system_generated`,`restrictions_apply`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`,`suppressed`)");
                commands.Add($"VALUES(0,1,3,{resourceId},{parentId},{parentName},{Position},1,'{refId}','{refId}','{_sleeve.SleeveTitleDate.title}','{displayStr2}'," +
                    $"893,'Sleeve',0,0,'admin','admin','{rdate}','{rdate}','{rdate}',0);SET @sleeveid = LAST_INSERT_ID();");

                if (!string.IsNullOrEmpty(_sleeve.SleeveTitleDate.expression))
                {
                    if (_sleeve.SleeveTitleDate.type == "901")
                    {
                        commands.Add("INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add($"VALUES(0, 1, @sleeveid,{_sleeve.SleeveTitleDate.type},906,'{_sleeve.SleeveTitleDate.expression}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                    else
                    {
                        commands.Add("INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`begin`,`end`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                        commands.Add($"VALUES(0, 1, @sleeveid,{_sleeve.SleeveTitleDate.type},906,'{_sleeve.SleeveTitleDate.expression}','{_sleeve.SleeveTitleDate.start}','{_sleeve.SleeveTitleDate.end}','admin','admin','{rdate}','{rdate}','{rdate}');");
                    }
                }
                commands.Add("INSERT INTO `ars_dev`.`instance`(`lock_version`,`json_schema_version`, `archival_object_id`,`instance_type_id`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                commands.Add($"VALUES(0, 1, @sleeveid, 353, 'admin','admin','{rdate}','{rdate}','{rdate}'); SET @instanceid = LAST_INSERT_ID();");

                commands.Add("INSERT INTO `ars_dev`.`sub_container`(`lock_version`,`json_schema_version`,`instance_id`,`type_2_id`,`indicator_2`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                commands.Add($"VALUES(0, 1, @instanceid, 320,'{_sleeve.number}','admin','admin','{rdate}','{rdate}','{rdate}'); SET @subid = LAST_INSERT_ID();");

                commands.Add("INSERT INTO `ars_dev`.`top_container_link_rlshp`(`top_container_id`,`sub_container_id`,`aspace_relationship_position`,`suppressed`,`system_mtime`,`user_mtime`)");
                commands.Add($"VALUES(@topid, @subid, 0, 0,'{rdate}','{rdate}');");
            }

            foreach (var component in _children)
            {
                if (isArchivalObject || parentType == "root") // || component.name == "item"
                    component.GetCommands(refId, resourceId, "sleeve", _sleeve.number, commands, ref lastBoxNum);
                else
                    component.GetCommands(refId, resourceId, parentType, _sleeve.number, commands, ref lastBoxNum);
            }

            return commands;
        }

        public override List<string> GetData(string refId, List<string> commands, ref string lastBoxNum)
        {
            commands.Add($"{_sleeve.id},Sleeve,{_sleeve.number},\"{_sleeve.title}\"");
            foreach (var component in _children)
            {
                component.GetData(refId, commands, ref lastBoxNum);
            }
            return commands;
        }
    }

    internal class Leaf : Component
    {
        private ItemModel _item;

        public Leaf(string name, ItemModel item) : base(name)
        {
            _item = item;
        }

        public override void Add(Component c)
        {
            Console.WriteLine("Cannot add to a leaf");
        }

        public override void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + name + Depth + ":" + Position);
        }

        public override void ReduceDepth(int amount)
        {
            Depth -= amount;
        }

        public override void SetPosition(int[] positions)
        {
            positions[0] += 1;
            if (positions[0] == 500 || (positions[0] - 500) % 1000 == 0) positions[0] += 1;
            Position = positions[0];
        }

        public override List<string> GetCommands(string refId, string resourceId, string parentType, string parentIndicator, List<string> commands, ref string lastBoxNum)
        {
            var isItemUnNumbered = false;
            if (_item.number.StartsWith("NULL"))
            {
                isItemUnNumbered = true;
                _item.number = _item.number.Replace("NULL", "");
            }
            refId += "-i" + _item.number;

            var parentId = string.Empty;
            var parentName = string.Empty;

            if (Depth == 1)
            {
                parentId = "NULL";
                parentName = $"'root@/repositories/3/resources/{resourceId}'";
            }
            else
            {
                switch (parentType)
                {
                    case "series":
                        parentId = "@seriesid";
                        parentName = "CONCAT(@seriesid, '@archival_object')";
                        break;
                    case "subseries":
                        parentId = "@subseriesid";
                        parentName = "CONCAT(@subseriesid, '@archival_object')";
                        break;
                    case "subsubseries":
                        parentId = "@subsubseriesid";
                        parentName = "CONCAT(@subsubseriesid, '@archival_object')";
                        break;
                    case "box":
                        parentId = "@boxid";
                        parentName = "CONCAT(@boxid, '@archival_object')";
                        break;
                    case "folder":
                        parentId = "@folderid";
                        parentName = "CONCAT(@folderid, '@archival_object')";
                        break;
                    case "sleeve":
                        parentId = "@sleeveid";
                        parentName = "CONCAT(@sleeveid, '@archival_object')";
                        break;
                }
            }

            //var parentId = Depth == 1 ? "NULL" : parentType == "series" ? "@seriesid" : parentType == "box" ? "@boxid" : parentType == "folder" ? "@folderid" : "";
            //var parentName = Depth == 1 ? $"'root@/repositories/3/resources/{resourceId}'" : parentType == "series" ? "CONCAT(@seriesid, '@archival_object')" : parentType == "box" ? "CONCAT(@boxid, '@archival_object')" : parentType == "folder" ? "CONCAT(@folderid, '@archival_object')" : "";

            var displayStr3 = string.IsNullOrEmpty(_item.ItemTitleDate.expression) ? _item.ItemTitleDate.title : _item.ItemTitleDate.title + ", " + _item.ItemTitleDate.expression;
            commands.Add("INSERT INTO `ars_dev`.`archival_object`(`lock_version`,`json_schema_version`,`repo_id`,`root_record_id`,`parent_id`,`parent_name`,`position`,`publish`,`ref_id`,`component_id`,`title`,`display_string`,`level_id`,`system_generated`,`restrictions_apply`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`,`suppressed`)");
            commands.Add($"VALUES(0,1,3,{resourceId},{parentId},{parentName},{Position},1,'{refId}','{refId}','{_item.ItemTitleDate.title}','{displayStr3}'," +
                $"892,0,0,'admin','admin','{rdate}','{rdate}','{rdate}',0);SET @itemid = LAST_INSERT_ID();");

            if (!string.IsNullOrEmpty(_item.ItemTitleDate.expression))
            {
                if (_item.ItemTitleDate.type == "901")
                {
                    commands.Add("INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                    commands.Add($"VALUES(0, 1, @itemid,{_item.ItemTitleDate.type},906,'{_item.ItemTitleDate.expression}','admin','admin','{rdate}','{rdate}','{rdate}');");
                }
                else
                {
                    commands.Add("INSERT INTO `ars_dev`.`date`(`lock_version`,`json_schema_version`,`archival_object_id`,`date_type_id`,`label_id`,`expression`,`begin`,`end`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                    commands.Add($"VALUES(0, 1, @itemid,{_item.ItemTitleDate.type},906,'{_item.ItemTitleDate.expression}','{_item.ItemTitleDate.start}','{_item.ItemTitleDate.end}','admin','admin','{rdate}','{rdate}','{rdate}');");
                }
            }

            commands.Add("INSERT INTO `ars_dev`.`instance`(`lock_version`,`json_schema_version`,`archival_object_id`,`instance_type_id`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
            commands.Add($"VALUES(0, 1, @itemid,353,'admin','admin','{rdate}','{rdate}','{rdate}'); SET @instanceid = LAST_INSERT_ID();");

            //317=box; 320=folder ; need parent type and value
            //1499=item;

            if (parentType == "box" || parentType == "root")
            {
                if (refId.Contains("-f"))
                {
                    var folderletter = refId.Replace("-f", "%").Split('%')[1].Split('-')[0];
                    commands.Add("INSERT INTO `ars_dev`.`sub_container`(`lock_version`,`json_schema_version`,`instance_id`,`type_2_id`,`indicator_2`,`type_3_id`,`indicator_3`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                    commands.Add($"VALUES(0, 1, @instanceid,320,'{folderletter}',1499,'{_item.number}','admin','admin','{rdate}','{rdate}','{rdate}'); SET @subid = LAST_INSERT_ID();");
                }
                else if(isItemUnNumbered)
                {
                    commands.Add("INSERT INTO `ars_dev`.`sub_container`(`lock_version`,`json_schema_version`,`instance_id`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                    commands.Add($"VALUES(0, 1, @instanceid,'admin','admin','{rdate}','{rdate}','{rdate}'); SET @subid = LAST_INSERT_ID();");
                }
                else
                {
                    commands.Add("INSERT INTO `ars_dev`.`sub_container`(`lock_version`,`json_schema_version`,`instance_id`,`type_2_id`,`indicator_2`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                    commands.Add($"VALUES(0, 1, @instanceid,1499,'{_item.number}','admin','admin','{rdate}','{rdate}','{rdate}'); SET @subid = LAST_INSERT_ID();");
                }
                
            }
            else
            {
                if (isItemUnNumbered)
                {
                    commands.Add("INSERT INTO `ars_dev`.`sub_container`(`lock_version`,`json_schema_version`,`instance_id`,`type_2_id`,`indicator_2`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                    commands.Add($"VALUES(0, 1, @instanceid,320,'{parentIndicator}','admin','admin','{rdate}','{rdate}','{rdate}'); SET @subid = LAST_INSERT_ID();");
                }
                else
                {
                    commands.Add("INSERT INTO `ars_dev`.`sub_container`(`lock_version`,`json_schema_version`,`instance_id`,`type_2_id`,`indicator_2`,`type_3_id`,`indicator_3`,`created_by`,`last_modified_by`,`create_time`,`system_mtime`,`user_mtime`)");
                    commands.Add($"VALUES(0, 1, @instanceid,320,'{parentIndicator}',1499,'{_item.number}','admin','admin','{rdate}','{rdate}','{rdate}'); SET @subid = LAST_INSERT_ID();");
                }
            }

            commands.Add("INSERT INTO `ars_dev`.`top_container_link_rlshp`(`top_container_id`,`sub_container_id`,`aspace_relationship_position`,`suppressed`,`system_mtime`,`user_mtime`)");
            commands.Add($"VALUES(@topid, @subid,0,0,'{rdate}','{rdate}');");

            return commands;
        }

        public override List<string> GetData(string refId, List<string> commands, ref string lastBoxNum)
        {
            if (_item.number.StartsWith("NULL"))
            {
                _item.number = _item.number.Replace("NULL", "");
            }

            commands.Add($"{_item.id},Item,{_item.number},\"{_item.title}\"");
            return commands;
        }
    }

}
