using System;
using UnityEngine;

namespace Topaz.Generation
{
    public enum WoodlandRole { Canopy, Undergrowth, Rock, Landmark, Camp, ResourceSite, ShallowWater, HarvestTree, HarvestRock }
    public enum WoodlandCollision { None, Solid, Gatherable, WalkableSurface }
    [Serializable]
    public sealed class WoodlandAssetRole
    {
        public WoodlandRole role;
        public GameObject prefab;
        public Vector3 groundAnchor;
        public Vector3 interactionAnchor;
        public Vector3 visualSize;
        public float footprint=1;
        public float clearance=1;
        public float maximumSlope=.35f;
        public Vector2 scaleRange=new Vector2(.8f,1.2f);
        public bool randomYaw=true;
        public WoodlandCollision collision;
        public bool developmentProxy;
        public void Validate()
        {
            if(prefab==null && role!=WoodlandRole.ShallowWater && role!=WoodlandRole.Canopy && role!=WoodlandRole.Undergrowth && role!=WoodlandRole.Rock)
                throw new InvalidOperationException("Missing required woodland binding: "+role);
            if(footprint<=0 || clearance<0 || maximumSlope<0 || scaleRange.x<=0 || scaleRange.y<scaleRange.x)
                throw new InvalidOperationException("Invalid woodland placement metadata: "+role);
        }
    }
}
