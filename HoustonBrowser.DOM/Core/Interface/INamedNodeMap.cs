using System;
using System.Collections;

namespace HoustonBrowser.DOM.Interface
{
    public interface INamedNodeMap: IEnumerable
    {
        Node GetNamedItem(string name);
        Node SetNamedItem(Node arg);                                       
        Node RemoveNamedItem(string name);                                          
    }
}