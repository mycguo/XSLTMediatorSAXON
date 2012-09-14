using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tridion.Extensions.Mediators
{
	internal class XsltHelperInfo
	{
		private object m_Helper;
		private string m_Namespace;

		public XsltHelperInfo(object helper, string helperNS)
		{
			m_Helper = helper;
			m_Namespace = helperNS;
		}

		public object HelperObject { get { return m_Helper; } }

		public string HelperNamespace { get { return m_Namespace; } }
	}
}
