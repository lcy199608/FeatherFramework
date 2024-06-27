using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParticleOrderInLayer : MonoBehaviour
{
    public int orderInLayer = 0;
    void Start()
    {
        gameObject.DescendantsAndSelf().ForEach(obj =>
        {
            var particle = obj.GetComponent<ParticleSystem>();
            if(particle != null)
            {
                var renderer = particle.GetComponent<Renderer>();
                if(renderer != null)
                {
                    renderer.sortingOrder = orderInLayer;
                }
            }
        });
    }
}
