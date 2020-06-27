﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
namespace Owl.Util
{
    public enum HAlign : int
    {
        Left,
        Right,
        Center
    }

    public enum VAlign : int
    {
        Top,
        Center,
        Bottom
    }
    /// <summary>
    /// 边框范围
    /// </summary>
    public enum BorderScope : int
    {
        None = 0,
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8,
        All = 15
    }

    public enum OpenType
    {
        CreateNew,
        OpenWrite,
        OpenRead
    }

    /// <summary>
    /// 单元格位置
    /// </summary>
    public class CellPosition
    {
        /// <summary>
        /// 行号，从1开始
        /// </summary>
        public int Row { get; private set; }
        /// <summary>
        /// 列号，从1开始
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// 获取列名称
        /// </summary>
        /// <param name="colName">起始列名称</param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected string GetColumnName(string colName, int count)
        {
            if (string.IsNullOrEmpty(colName))
                colName = "A";
            if (count == 0)
                return colName;
            int start = (int)'A';
            char tmpend = colName[colName.Length - 1];
            string otherchars = colName.Length == 1 ? "" : colName.Substring(0, colName.Length - 1);
            int endno = (int)tmpend + count;
            int mulp = (endno - start) / 26;
            int yushu = (endno - start) % 26;
            string tmp = "";
            if (mulp > 0)
            {
                tmp = string.Format("{0}{1}", GetColumnName(otherchars, mulp - 1), (char)(start + yushu));
            }
            else
                tmp = string.Format("{0}{1}", otherchars, (char)(start + yushu));

            return tmp;
        }
        string m_addressname;
        /// <summary>
        /// 单元格的位置
        /// </summary>
        public string AddressName
        {
            get
            {
                if (string.IsNullOrEmpty(m_addressname))
                    m_addressname = string.Format("{0}{1}", GetColumnName("A", Column - 1), Row);
                return m_addressname;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row">行号，从1开始</param>
        /// <param name="column">列号，从1开始</param>
        public CellPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }
    }

    public abstract class ExcelHelper : IDisposable
    {
        static ExcelHelper CreateHelper()
        {
            return ObjectContainer.Instance.Resolve<ExcelHelper>();
        }

        /// <summary>
        /// 创建excel帮助类
        /// </summary>
        /// <param name="filename">要保存的文件路径</param>
        /// <returns></returns>
        public static ExcelHelper CreateHelper(string filename, OpenType type = OpenType.CreateNew)
        {
            ExcelHelper helper = CreateHelper();
            switch (type)
            {
                case OpenType.CreateNew: helper.OpenNew(filename); break;
                case OpenType.OpenWrite: helper.doOpenWrite(filename); break;
                case OpenType.OpenRead: helper.doOpenRead(filename); break;
            }

            return helper;
        }
        /// <summary>
        /// 创建excel帮助类
        /// </summary>
        /// <param name="templatpath">模板路径</param>
        /// <param name="filename">要创建的文件路径</param>
        /// <returns></returns>
        public static ExcelHelper CreateHelper(string templatpath, string filename)
        {
            ExcelHelper helper = CreateHelper();
            helper.OpenWrite(templatpath, filename);
            return helper;
        }

        static Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        void OpenWrite(string templatpath, string filename)
        {
            byte[] bytes;
            lock (files)
            {
                if (!files.ContainsKey(templatpath))
                {
                    using (FileStream stream = File.OpenRead(templatpath))
                    {
                        bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, (int)stream.Length);
                        files[templatpath] = bytes;
                    }
                }
                bytes = files[templatpath];
            }
            FileHelper.Create(filename, bytes);
            doOpenWrite(FileHelper.GetFilePath(filename));
        }
        public void OpenNew(string filename)
        {
            doOpenNew(filename);
        }
        protected abstract void doOpenWrite(string filepath);
        protected abstract void doOpenNew(string filepath);

        protected abstract void doOpenRead(string filepath);
        /// <summary>
        /// 获取列名称
        /// </summary>
        /// <param name="colName">起始列名称</param>
        /// <param name="count"></param>
        /// <returns></returns>
        public string GetColumn(string colName, int count)
        {
            if (string.IsNullOrEmpty(colName))
                colName = "A";
            int start = (int)'A';
            char tmpend = colName[colName.Length - 1];
            string otherchars = colName.Length == 1 ? "" : colName.Substring(0, colName.Length - 1);
            int endno = (int)tmpend + count;
            int mulp = (endno - start) / 26;
            int yushu = (endno - start) % 26;
            string tmp = "";
            if (mulp > 0)
            {
                tmp = string.Format("{0}{1}", GetColumn(otherchars, mulp - 1), (char)(start + yushu));
            }
            else
                tmp = string.Format("{0}{1}", otherchars, (char)(start + yushu));

            return tmp;
        }
        /// <summary>
        /// 创建工作表
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        public abstract void CreateSheet(string sheetname);
        /// <summary>
        /// 复制工作表
        /// </summary>
        /// <param name="orgsheet">原工作表</param>
        /// <param name="sheetname">复制到的工作表</param>
        public abstract void CreateSheet(string orgsheet, string sheetname);
        /// <summary>
        /// 删除工作表
        /// </summary>
        /// <param name="sheet">表名称</param>
        public abstract void DeleteSheet(string sheet);
        /// <summary>
        /// 读取指定工作表上指定位置的值
        /// </summary>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="position">单元格的位置如 A1</param>
        /// <returns></returns>
        public abstract string ReadText(string sheetName, CellPosition position);

        /// <summary>
        /// 向指定工作表的指定位置写入值
        /// </summary>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="position">单元格的位置如 A1</param>
        /// <param name="value">值</param>
        /// <param name="border">边框样式</param>
        /// <param name="fontname">字体名称</param>
        /// <param name="fontsize">字体大小</param>
        /// <param name="align">水平对齐方式</param>
        /// <param name="valign">垂直对齐方式</param>
        public abstract void WriteTo(string sheetName, CellPosition position, object value, BorderScope border = BorderScope.All, string fontname = "微软雅黑", int fontsize = 10, HAlign? align = HAlign.Left, VAlign? valign = VAlign.Center);

        public virtual void WriteTo(string sheetName, int row, int col, object value, BorderScope border = BorderScope.All, string fontname = "微软雅黑", int fontsize = 10, HAlign? align = HAlign.Left, VAlign? valign = VAlign.Center)
        {
            WriteTo(sheetName, new CellPosition(row, col), value, border, fontname, fontsize, align, valign);
        }
        /// <summary>
        /// 向指定工作表的指定位置写入公式
        /// </summary>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="position">单元格的位置如 A1</param>
        /// <param name="formula">公式</param>
        public abstract void WriteFormula(string sheetName, CellPosition position, string formula);

        public virtual void WriteFormula(string sheetName, int row, int col, string formula)
        {
            WriteFormula(sheetName, new CellPosition(row, col), formula);
        }
        /// <summary>
        /// 向指定工作表的指定行插入字体为 微软雅黑 字号 为10 有边框的行
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="rowindex">行号从1开始</param>
        /// <param name="values">值集合</param>
        public void InsertRow(string sheetname, int rowindex, params object[] values)
        {
            InsertRow(sheetname, rowindex, "微软雅黑", 10, true, values);
        }
        /// <summary>
        /// 向指定工作表的指定行插入字体为 微软雅黑 字号 为10 的行
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="rowindex">行号从1开始</param>
        /// <param name="hasborder">是否有边框</param>
        /// <param name="values">值集合</param>
        public void InsertRow(string sheetname, int rowindex, bool hasborder, params object[] values)
        {
            InsertRow(sheetname, rowindex, "微软雅黑", 10, hasborder, values);
        }

        /// <summary>
        /// 向指定工作表的指定行插入行
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="rowindex">行号从1开始</param>
        /// <param name="fontname">字体名称</param>
        /// <param name="fontsize">字体大小</param>
        /// <param name="hasborder">是否有边框</param>
        /// <param name="values">值集合</param>
        public abstract void InsertRow(string sheetname, int rowindex, string fontname, int fontsize, bool hasborder, params object[] values);

        /// <summary>
        /// 向指定工作表的指定行插入字体为 微软雅黑 字号 为10 有边框的行
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="rowindex">行号从1开始</param>
        /// <param name="values">值集合</param>
        public void WriteRow(string sheetname, int rowindex, params object[] values)
        {
            WriteRow(sheetname, rowindex, "微软雅黑", 10, true, values);
        }
        /// <summary>
        /// 向指定工作表的指定行插入字体为 微软雅黑 字号 为10 的行
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="rowindex">行号从1开始</param>
        /// <param name="hasborder">是否有边框</param>
        /// <param name="values">值集合</param>
        public void WriteRow(string sheetname, int rowindex, bool hasborder, params object[] values)
        {
            WriteRow(sheetname, rowindex, "微软雅黑", 10, hasborder, values);
        }

        /// <summary>
        /// 向指定工作表的指定行写入数据
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="rowindex">行号从1开始</param>
        /// <param name="fontname">字体名称</param>
        /// <param name="fontsize">字体大小</param>
        /// <param name="hasborder">是否有边框</param>
        /// <param name="values">值集合</param>
        public abstract void WriteRow(string sheetname, int rowindex, string fontname, int fontsize, bool hasborder, params object[] values);
        /// <summary>
        /// 向指定工作表中写入数据,行间用 \n 分割
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="source">输入内容</param>
        /// <param name="sep">分割符</param>
        /// <param name="fontname">字体名称</param>
        /// <param name="fontsize">字体大小</param>
        /// <param name="hasborder">是否有边框</param>
        public virtual void WriteSheet(string sheetname, string source, char sep, string fontname = "微软雅黑", int fontsize = 10, bool hasborder = true)
        {
            int i = 1;
            foreach (var row in source.Split('\n'))
            {
                InsertRow(sheetname, i, fontname, fontsize, hasborder, row.Split(sep));
                i = i + 1;
            }
        }

        /// <summary>
        /// 读取指定sheet的内容
        /// </summary>
        /// <param name="sheetName">为空则取第一个sheet</param>
        /// <returns></returns>
        public abstract List<string[]> ReadSheet(string sheetName = null);

        /// <summary>
        /// 合并单元格
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="positionstart">起始位置</param>
        /// <param name="positionend">终止位置</param>
        /// <param name="align">合并后水平对齐方式</param>
        /// <param name="valign">合并后垂直对齐方式</param>
        public abstract void MergeCell(string sheetname, CellPosition positionstart, CellPosition positionend, HAlign? align = HAlign.Center, VAlign? valign = VAlign.Center);

        /// <summary>
        /// 合并单元格
        /// </summary>
        /// <param name="sheetname">工作表名称</param>
        /// <param name="firstrow">第一行（开始序号为1）</param>
        /// <param name="lastrow">最后行（开始序号为1）</param>
        /// <param name="firsrcol">第一列（开始序号为1）</param>
        /// <param name="lastcol">最后列（开始序号为1）</param>
        /// <param name="align">合并后水平对齐方式</param>
        /// <param name="valign">合并后垂直对齐方式</param>
        public virtual void MergeCell(string sheetname, int firstrow, int lastrow, int firsrcol, int lastcol, HAlign? align = HAlign.Center, VAlign? valign = VAlign.Center)
        {
            MergeCell(sheetname, new CellPosition(firstrow, firsrcol), new CellPosition(lastrow, lastcol), align, valign);
        }
        public abstract void Dispose();
    }

}
