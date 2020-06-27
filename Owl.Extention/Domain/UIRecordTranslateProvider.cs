using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Domain.Driver;
using Owl.Feature;
using Owl.Util;
namespace Owl.PL
{
    public class UIRecordTranslateProvider : RecordTranslateProvider
    {
        string GetSelectText(SelectField field, string value, string[] depvalues)
        {
            if (value == null) return "";
            if (!field.Multiple)
                return field.GetText(value, depvalues);
            return string.Join(",", value.Split(',').Select(s => field.GetText(s, depvalues)));
        }

        void TranslateM2O(Many2OneField navfield, string prefix, string rname, TransferObject record, TransferObject parent)
        {
            var obj = new object[2];
            var keyname = string.IsNullOrEmpty(prefix) ? navfield.GetFieldname() : rname;
            if (!record.ContainsKey(keyname))
            {
                record[rname] = obj;
                return;
            }
            obj[0] = record[keyname];
            if (navfield.IsSingle)
                obj[1] = obj[0];
            else
            {
                List<string> tmpdis = new List<string>();
                foreach (var disfied in navfield.RelationDisField)
                {
                    var tmp = navfield.RelationModelMeta.GetField(disfied);
                    if (tmp == null)
                    {
                        tmpdis.Add("");
                        continue;
                    }
                    var key = string.Format("{0}_{1}", rname, disfied.Replace('.', '_'));
                    if (tmp.Field_Type == FieldType.many2one)
                        key = string.Format("{0}_{1}", key, (tmp as NavigatField).RelationDisField[0]);
                    if (disfied == navfield.RelationField)
                        key = keyname;
                    var value = record.GetRealValue<string>(key);
                    if (record.ContainsKey(key))
                        record.Remove(key);
                    if (tmp.Field_Type == FieldType.select)
                    {
                        var depvalues = tmp.GetDomainField().Dependence.Select(s => record.GetRealValue<string>(string.Format("{0}_{1}", rname, s.Replace('.', '_')))).ToArray();
                        tmpdis.Add(GetSelectText(tmp as SelectField, value, depvalues));
                    }
                    else
                        tmpdis.Add(value);
                }
                if (tmpdis.Where(s => !string.IsNullOrEmpty(s)).Count() == 0)
                {
                    obj[1] = navfield.GetDomainField().DefaultDisplayText;
                }
                else
                    obj[1] = string.IsNullOrEmpty(navfield.Format) ? string.Join(Const.CoreConst.SelectDisplaySeparator, tmpdis) : string.Format(navfield.Format, tmpdis.ToArray());
                record[rname + "__org__"] = tmpdis;
            }
            if (!navfield.CanIgnore)
                record.Remove(keyname);
            record[rname] = obj;
        }

        void TranslateSelect(SelectField field, string prefix, string rname, TransferObject record, TransferObject parent)
        {
            if (record.ContainsKey(rname))
            {
                var value = record.GetRealValue<string>(rname);
                var depvalues = new string[field.GetDomainField().Dependence.Length];

                for (var i = 0; i < field.GetDomainField().Dependence.Length; i++)
                {
                    var depfield = field.GetDomainField().Dependence[i].Replace('.', '_');
                    if (!string.IsNullOrEmpty(prefix))
                        depfield = string.Format("{0}_{1}", prefix, depfield);
                    if (depfield.StartsWith("TopObj") && parent != null)
                    {
                        depvalues[i] = parent.GetRealValue<string>(depfield.Substring(7));
                    }
                    else if (record.ContainsKey(depfield))
                        depvalues[i] = record.GetRealValue<string>(depfield);
                }
                var text = GetSelectText(field, value, depvalues);
                if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(text))
                    text = field.GetDomainField().DefaultDisplayText;
                record[rname] = new object[2] { value, text ?? value };
            }
        }
        public override void Translate(FieldMetadata field, string m2ofield, TransferObject record, TransferObject parent)
        {
            var fieldname = "";
            var rname = "";
            var prefix = "";
            if (!string.IsNullOrEmpty(m2ofield))
            {
                prefix = field.Name;
                fieldname = string.Format("{0}_{1}", field.Name, m2ofield.Replace('.', '-'));
                rname = fieldname;
                field = (field as NavigatField).RelationModelMeta.GetField(m2ofield);
            }
            else
            {
                fieldname = field.GetFieldname();
                rname = field.Name;
            }
            if (!record.ContainsKey(fieldname))
                return;
            var value = record[fieldname];
            switch (field.Field_Type)
            {
                case FieldType.date:
                    record[rname] = value == null ? null : Convert2.ChangeType<DateTime>(value).ToString("yyyy-MM-dd");
                    break;
                case FieldType.datetime:
                    record[rname] = value == null ? null : ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                case FieldType.digits:
                case FieldType.number:
                    record[rname] = value == null ? null : string.Format(field.Format.Coalesce("{0:N" + field.GetDomainField().Precision + "}"), value);
                    break;
                case FieldType.many2one:
                    TranslateM2O((Many2OneField)field, prefix, rname, record, parent);
                    break;
                case FieldType.select:
                    TranslateSelect((SelectField)field, prefix, rname, record, parent);
                    break;
                case FieldType.file:
                    if (!string.IsNullOrEmpty((string)value))
                    {
                        var obj = new object[2];
                        obj[0] = value;
                        obj[1] = "";
                        if (value != null)
                            obj[1] = string.Join(",", Attach.GetName(((string)value).Split(',')));
                        record[rname] = obj;
                    }
                    break;
                default:
                    if (field.GetDomainField().TranslateValue && value != null && value.ToString() != "")
                    {
                        var obj = new object[2];
                        obj[0] = value.ToString();
                        obj[1] = Translation.Get(field.GetDomainField().TranslateValueResKey, value.ToString(), true);
                        record[rname] = obj;
                    }
                    break;
            }
            if (field.Field_Type != FieldType.many2one && !string.IsNullOrEmpty(field.Format))
                record[rname] = string.Format(field.Format, value);
        }

        public override int Priority
        {
            get { return 10000; }
        }
    }
}
