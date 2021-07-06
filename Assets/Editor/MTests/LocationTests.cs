using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Class;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Assets.Editor.MTests
{
    [TestFixture]
    public class LocationTests : MonoBehaviour
    {
        [Test]
        [TestCase(1, 2, 3)]
        public void RemoveZ_Float3IsPassed_ReturnFloat2(float x, float y, float z)
        {
            var testFloat = new float3(x, y, z);
            var expectedFloat = new float2(x, y);

            var result = Location.RemoveZ(new float3(testFloat));

            Assert.That(result, Is.EqualTo(expectedFloat));
        }

        [Test]
        public void HasMatch_WhenThereIsMatch_ReturnTrue()
        {
            var matchTranslation = new Translation();
            matchTranslation.Value = new float3(1, 2, 3);

            var testArray = new NativeArray<Translation>(3, Allocator.Temp);
            testArray[0] = new Translation();
            testArray[1] = new Translation();
            testArray[2] = matchTranslation;

            var result = Location.HasMatch(testArray, matchTranslation);

            Assert.That(result, Is.True);

            testArray.Dispose();
        }

        [Test]
        public void HasMatch_WhenThereIsNoMatch_ReturnFalse()
        {
            var matchTranslation = new Translation();
            matchTranslation.Value = new float3(1, 2, 3);

            var testArray = new NativeArray<Translation>(3, Allocator.Temp);
            testArray[0] = new Translation();
            testArray[1] = new Translation();
            testArray[2] = new Translation();

            var result = Location.HasMatch(testArray, matchTranslation);

            Assert.That(result, Is.False);

            testArray.Dispose();
        }

        //TODO: Write test for GetMatchedEntity

        [Test]
        public void IsMatchLocation_ThereIsMatch_ReturnTrue()
        {
            float3 testLocation = new float3(1, 2, 3);
            float3 testLocation2 = new float3(1, 2, 3);

            var result = Location.IsMatchLocation(testLocation, testLocation2);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsMatchLocation_ThereIsNoMatch_ReturnFalse()
        {
            float3 testLocation = new float3(2, 1, 3);
            float3 testLocation2 = new float3(float3.zero);

            var result = Location.IsMatchLocation(testLocation, testLocation2);

            Assert.That(result, Is.False);
        }
    }
}
