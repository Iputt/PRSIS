using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain.Driver
{
    public abstract class RecordTranslateProvider : Provider
    {
        public abstract void Translate(FieldMetadata meta,string m2ofield, TransferObject record, TransferObject parent);
    }

    public class GeneralRecordTranslateProvider : RecordTranslateProvider
    {
        public override int Priority
        {
            get { return 1; }
        }
        public override void Translate(FieldMetadata meta,string m2ofield, TransferObject record, TransferObject parent)
        {

        }
    }

    /// <summary>
    /// 数据翻译器
    /// </summary>
    public class RecordTranslator : Engine<RecordTranslateProvider, RecordTranslator>
    {
        protected override EngineMode Mode
        {
            get
            {
                return EngineMode.Single;
            }
        }
        /// <summary>
        /// 进行翻译
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="record"></param>
        public static void Translate(FieldMetadata meta, string m2ofield, TransferObject record, TransferObject parent)
        {
            Provider.Translate(meta,m2ofield, record, parent);
        }
    }
}
