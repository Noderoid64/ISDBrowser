using System;
using HoustonBrowser.DOM.Interface;

namespace HoustonBrowser.DOM
{
    public class EntityReference : Node
    {
        public EntityReference(string nameEntityRef) :
            base(TypeOfNode.ENTITY_REFERENCE_NODE, nameEntityRef, null)
        { }
    }
}