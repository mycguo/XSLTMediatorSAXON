using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;

namespace Tridion.Extensions.Mediators
{
	/// <summary>
	/// Customized XmlUrlResolver to load the XML SDLTridion Items by URI
	/// </summary>
	public class TcmUriResolver : XmlUrlResolver
	{
		#region Private Members
		private TemplatingLogger m_Logger = null;
        private Engine m_Engine;
		#endregion

		public TcmUriResolver(Engine templateEngine)
        {
            m_Engine = templateEngine;
			m_Logger = TemplatingLogger.GetLogger(this.GetType());
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            switch (absoluteUri.Scheme)
            {
                case "tcm":					
					IdentifiableObject objTridionItem = m_Engine.GetObject(absoluteUri.ToString()); //Retrieve the object as a generic item

					string xml;

					if (objTridionItem is TemplateBuildingBlock)					
						xml = ((TemplateBuildingBlock)objTridionItem).Content;				
					else
						xml = objTridionItem.ToXml().OuterXml;

					MemoryStream memStream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

					memStream.Position = 0; //Reset the stream's position

					return memStream; //Return the stream
                default:
					return base.GetEntity(absoluteUri, role, ofObjectToReturn); //for all other schemes, use the default handler of System.Xml.XmlUrlResolver
            }
        }
	}
}