﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

namespace ASCOM.Utilities.CS
{

/// <summary>
/// ''' Class that represents a whole device Profile and which contains a set of methods for for manipulating its contents
/// ''' </summary>
/// ''' <remarks>
/// ''' This class is used by the Profile.GetProfile and Profile.SetProfile methods, do not confuse it with the Profile Class itself.
/// ''' </remarks>
[ComVisible(true)]
[Guid("43325B3A-8B34-48db-8028-9D8CED9FA9E2")]
[ClassInterface(ClassInterfaceType.None)]
public class ASCOMProfile : IXmlSerializable
{
    private SortedList<string, SortedList<string, string>> Subkey;

    public ASCOMProfile()
    {
        Subkey = new SortedList<string, SortedList<string, string>>();
    }

    /// <summary>
    ///     ''' Add a new subkey
    ///     ''' </summary>
    ///     ''' <param name="SubKeyName">Name of the subkey</param>
    ///     ''' <remarks></remarks>
    public void AddSubkey(string SubKeyName)
    {
        try
        {
            Subkey.Add(SubKeyName, new SortedList<string, string>());
            SetValue(SubKeyName, "", ""); // Set the default value to uninitialised
        }
        catch (ArgumentException ex)
        {
        }// Ignore this exception which occurs when the subkey has already been added
    }

    /// <summary>
    ///     ''' Clears all contents
    ///     ''' </summary>
    ///     ''' <remarks></remarks>
    public void Clear()
    {
        Subkey.Clear();
    }

    /// <summary>
    ///     ''' Retrieve a registry value
    ///     ''' </summary>
    ///     ''' <param name="Name">Name of the value</param>
    ///     ''' <param name="SubKeyName">"SubKey in which the value is located</param>
    ///     ''' <returns>String value</returns>
    ///     ''' <remarks></remarks>
    public new string GetValue(string Name, string SubKeyName)
    {
        return Subkey[SubKeyName].Item[Name];
    }

    /// <summary>
    ///     ''' Removes a complete subkey
    ///     ''' </summary>
    ///     ''' <param name="SubKeyName">Subkey to be removed</param>
    ///     ''' <remarks></remarks>
    public void RemoveSubKey(string SubKeyName)
    {
        try
        {
            Subkey.Remove(SubKeyName);
        }
        catch (Exception ex)
        {
        }
    }

    /// <summary>
    ///     ''' Remove a value
    ///     ''' </summary>
    ///     ''' <param name="ValueName">Name of the value to be removed</param>
    ///     ''' <param name="SubKeyName">"SubKey containing the value</param>
    ///     ''' <remarks></remarks>
    public new void RemoveValue(string ValueName, string SubKeyName)
    {
        if (ValueName != "")
        {
            try
            {
                Subkey[SubKeyName].Remove(ValueName); // Catch exception oif they value doesn't exist
            }
            catch (Exception ex)
            {
            }
        }
    }

    /// <summary>
    ///     ''' Set a value
    ///     ''' </summary>
    ///     ''' <param name="Name">Name of the value to set</param>
    ///     ''' <param name="Value">Value to be set</param>
    ///     ''' <param name="SubKeyName">"Subkey continaining the value</param>
    ///     ''' <remarks>Changing a value with this method does NOT change the underlying profile store, only the value in this class.
    ///     ''' In order to persist the new value, the class should be written back to the profile store through Profile.SetProfile.</remarks>
    public new void SetValue(string Name, string Value, string SubKeyName)
    {
        if (Subkey.ContainsKey(SubKeyName))
            Subkey[SubKeyName].Item[Name] = Value;
        else
        {
            Subkey.Add(SubKeyName, new SortedList<string, string>());
            Subkey[SubKeyName].Add("", ""); // Set the default value to unset
            Subkey[SubKeyName][Name] = Value;
        }
    }

    /// <summary>
    ///     ''' Get the collection of values in this profile
    ///     ''' </summary>
    ///     ''' <value>All values in the collection</value>
    ///     ''' <returns>SortedList(Of SubKey as String, SortedList(Of ValueName as String, Value as String))</returns>
    ///     ''' <remarks></remarks>
    [ComVisible(false)]
    public SortedList<string, SortedList<string, string>> ProfileValues
    {
        get
        {
            return Subkey;
        }
    }

    /// <summary>
    ///     ''' Retrieve a registry value from the driver top level subkey
    ///     ''' </summary>
    ///     ''' <param name="Name">Name of the value </param>
    ///     ''' <returns>String value</returns>
    ///     ''' <remarks></remarks>
    [ComVisible(false)]
    public new string GetValue(string Name)
    {
        return GetValue(Name, "");
    }

    /// <summary>
    ///     ''' Remove a value from the driver top level subkey
    ///     ''' </summary>
    ///     ''' <param name="ValueName">Name of the value to be removed</param>
    ///     ''' <remarks></remarks>
    [ComVisible(false)]
    public new void RemoveValue(string ValueName)
    {
        RemoveValue(ValueName, "");
    }

    /// <summary>
    ///     ''' Set a value in the driver top level subkey
    ///     ''' </summary>
    ///     ''' <param name="Name">Name of the value to set</param>
    ///     ''' <param name="Value">Value to be set</param>
    ///     ''' <remarks></remarks>
    [ComVisible(false)]
    public new void SetValue(string Name, string Value)
    {
        SetValue(Name, Value, "");
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public System.Xml.Schema.XmlSchema GetSchema()
    {
        return (null/* TODO Change to default(_) if this is not a reference type */);
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void ReadXml(System.Xml.XmlReader reader)
    {
        string CurrentSubKey = "";
        string CurrentName = "";
        Subkey.Clear(); // Make sure we are starting with an empty collection in case the user has already played with this object
        while (reader.Read()) // Read the xml stream
        {
            switch (reader.Name) // Determine what to do based on the element name
            {
                case object _ when XML_SUBKEYNAME_ELEMENTNAME // This is a subkey element so get itand save for future use when adding values
               :
                    {
                        CurrentSubKey = reader.ReadString();
                        Subkey.Add(CurrentSubKey, new SortedList<string, string>()); // Create a new 
                        break;
                    }

                case object _ when XML_DEFAULTVALUE_ELEMENTNAME // Default element value so add this to the collection
         :
                    {
                        Subkey[CurrentSubKey].Add("", reader.ReadString()); // Set the default value to unset
                        break;
                    }

                case object _ when XML_NAME_ELEMENTNAME:
                    {
                        CurrentName = reader.ReadString(); // This is a value name so save it for when we get a value element
                        break;
                    }

                case object _ when XML_DATA_ELEMENTNAME:
                    {
                        Subkey[CurrentSubKey].Add(CurrentName, reader.ReadString()); // This is a value element so add it using the saved subkey and name
                        break;
                    }

                default:
                    {
                        break;
                    }
            }
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void WriteXml(System.Xml.XmlWriter writer)
    {
        foreach (string key in Subkey.Keys)
        {
            writer.WriteStartElement(XML_SUBKEY_ELEMENTNAME); // Start the SubKey element
            writer.WriteElementString(XML_SUBKEYNAME_ELEMENTNAME, key); // Write the subkey name
            writer.WriteElementString(XML_DEFAULTVALUE_ELEMENTNAME, Subkey[key].Item[""]); // Write the default value
            writer.WriteStartElement(XML_VALUES_ELEMENTNAME); // Start the values element
            foreach (Generic.KeyValuePair<string, string> kvp in Subkey[key]) // Write name values pairs except for default value
            {
                if (!string.IsNullOrEmpty(kvp.Key))
                {
                    writer.WriteStartElement(XML_VALUE_ELEMENTNAME);
                    writer.WriteElementString(XML_NAME_ELEMENTNAME, kvp.Key);
                    writer.WriteElementString(XML_DATA_ELEMENTNAME, kvp.Value);
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement(); // Close the values element
            writer.WriteEndElement(); // Close the subkey element
        }
    }
}
}