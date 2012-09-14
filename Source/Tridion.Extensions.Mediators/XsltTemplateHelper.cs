using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tridion.ContentManager.Templating;
using System.Xml;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager;
using System.IO;

namespace Tridion.Extensions.Mediators
{
	/// <summary>
	/// Provides functionality to xslt templates by exposing methods that can be called 
	/// during the transformation.
	/// </summary>
	public class XsltTemplateHelper
	{
		#region Private Members

		private TemplatingLogger m_Logger = null;
		private Engine m_Engine;
		#endregion

		#region Constructors

		public XsltTemplateHelper(Engine templateEngine)
		{
			m_Logger = TemplatingLogger.GetLogger(this.GetType());
			m_Engine = templateEngine;
		}
		#endregion

		#region Methods

		/// <summary>
		///		Generic XSLT helper function to format date strings using standard format strings
		///		Date returned as a string
		/// <example>
		/// <xsl:value-of select="txh:getFormatedDate(//tcm:CreationDate, 'dddd, MMM dd, yyyy')"/>
		/// </example>
		/// </summary>
		public String getFormatedDate(String inputDate, String dateFormat)
		{
			m_Logger.Debug("Input date is:" + inputDate);

			DateTime date = DateTime.ParseExact(inputDate, "s", null);
			String returnDate = date.ToString(dateFormat);
	
			m_Logger.Debug("Return date is:" + returnDate);
			return returnDate;
		}

		/// <summary>
		/// Generic XSLT helper function to retrieve items from within an OrganizationalItem
		///List returned as an XmlDocument
		///		<example> 
		///			<xsl:variable name="FolderItems" select="txh:getListItems(//tcm:OrganizationalItem/@xlink:href)"/>
		///				<xsl:for-each select="$FolderItems//tcm:Item[@Type='16']">
		///					<xsl:value-of select="@Title"/><br/>
		///				</xsl:for-each>
		///		</example>
		/// </summary>		
		/// <param name="uriOrgItem">tcm uri of the organizational item</param>
		/// <returns>xmldocument containing a list of child items for the org item</returns>
		public XmlDocument getListItems(String uriOrgItem)
		{
			m_Logger.Debug("Retriving item for the uri:" + uriOrgItem);

			XmlDocument getListItemsDOM = new XmlDocument(); //Create an XmlDocument to hold the list

			try
			{
				OrganizationalItem orgItem = (OrganizationalItem)m_Engine.GetObject(uriOrgItem); //Attempt to load the item into an OrganizationalItem object
				
				switch (orgItem.Id.ItemType)//Load the XML for the valid OrgItem types
				{
					case ItemType.Folder:
					case ItemType.StructureGroup:
					case ItemType.Category:						
						getListItemsDOM.LoadXml(orgItem.GetListItems().OuterXml);
						break;
					default:
						m_Logger.Warning("Items of the type '" + orgItem.GetType().FullName + "' are not currently supported with this Version of the XSLT Mediators's XSLT Template Helper extention");
						break;
				}
			}
			catch
			{				
				m_Logger.Error("The item represented by the uri '" + uriOrgItem + "' is not a valid OrganizationalItem for the getListItems method of the XSLT Template Helper");
			}

			return getListItemsDOM; //Return the list of items as an XMlDocument
		}

		/// <summary>
		/// Returns xml with information about the MM component such as the mime type or extension
		/// </summary>
		/// <param name="uri">tcm uri of a multimedia component</param>
		public XmlDocument GetMultimediaInfo(string uri)
		{
			StringBuilder sb = new StringBuilder();
			XmlDocument result = new XmlDocument();

			try
			{
				Component comp = m_Engine.GetObject(uri) as Component;

				m_Logger.Debug("GetMultimediaInfo: got component = " + uri);

				if (comp.ComponentType == ComponentType.Multimedia)
				{
					using (StringWriter writer = new StringWriter(sb))
					{
						using (XmlTextWriter xwriter = new XmlTextWriter(writer))
						{
							xwriter.WriteStartDocument(true);
							xwriter.WriteStartElement("multimedia");

							m_Logger.Debug("GetMultimediaInfo: mm type: " + comp.BinaryContent.MultimediaType.MimeType);

							xwriter.WriteElementString("type", comp.BinaryContent.MultimediaType.MimeType);

							m_Logger.Debug("GetMultimediaInfo: mm size: " + GetFileSize(comp.BinaryContent.FileSize));

							xwriter.WriteElementString("size", GetFileSize(comp.BinaryContent.FileSize));

							IList<string> exts = (IList<string>)comp.BinaryContent.MultimediaType.FileExtensions;

							if (exts.Count > 0)
							{
								xwriter.WriteElementString("ext", exts[0].ToUpper());
							}

							xwriter.WriteEndElement();//multimedia
							xwriter.WriteEndDocument();

							xwriter.Flush();

							result.LoadXml(sb.ToString());
						}
					}
				}
			}
			catch (Exception ex)
			{
				m_Logger.Error("GetMultimediaInfo failed!", ex);
			}

			return result;
		}

		private string GetFileSize(int bits)
		{
			string res;

			int bytes = bits / 8;

			if (bytes > 1024)
			{
				res = String.Format("{0}MB", bytes / 1024);
			}
			else
				res = String.Format("{0}KB", bytes);

			return res;
		}

		#endregion
	}
}