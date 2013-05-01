﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyCouch.Querying;
using MyCouch.Testing;
using MyCouch.Testing.Model;
using NUnit.Framework;

namespace MyCouch.IntegrationTests.Views
{
    [TestFixture]
    public class ViewsTests : IntegrationTestsOf<IViews>
    {
        protected Artist[] Artists;

        protected override void OnFixtureInitialize()
        {
            base.OnFixtureInitialize();

            Artists = TestData.Artists.CreateArtists(10);
            
            var tasks = new List<Task>();
            tasks.AddRange(Artists.Select(item => Client.Documents.PostAsync(item)));
            tasks.Add(Client.Documents.PostAsync(TestData.Views.ArtistsAlbums));
            
            Task.WaitAll(tasks.ToArray());
        }

        protected override void OnTestInitialize()
        {
            base.OnTestInitialize();

            SUT = Client.Views;
        }

        protected override void OnFixtureFinalize()
        {
            base.OnFixtureFinalize();

            IntegrationTestsRuntime.ClearAllDocuments();
        }

        [Test]
        public void When_Skipping_2_of_10_Then_8_rows_are_returned()
        {
            var query = new ViewQuery("artists", "albums").Configure(cfg => cfg.Skip(2));

            var response = SUT.RunQuery<Album[]>(query);

            var expected = GetExpectedAlbums(a => a.Skip(2));
            response.Should().BeSuccessfulGet(expected);
        }

        [Test]
        public void When_Limit_to_2_Then_2_rows_are_returned()
        {
            var query = new ViewQuery("artists", "albums").Configure(cfg => cfg.Limit(2));
            
            var response = SUT.RunQuery<Album[]>(query);

            var expected = GetExpectedAlbums(a => a.Take(2));
            response.Should().BeSuccessfulGet(expected);
        }

        [Test]
        public void When_Key_is_specified_Then_matching_row_is_returned()
        {
            var artist = Artists[2];
            var query = new ViewQuery("artists", "albums").Configure(cfg => cfg.Key(artist.Name));

            var response = SUT.RunQuery<Album[]>(query);

            response.Should().BeSuccessfulGet(new [] { artist.Albums });
        }

        [Test]
        public void When_Keys_are_specified_Then_matching_rows_are_returned()
        {
            var artists = Artists.Skip(2).Take(3).ToArray();
            var keys = artists.Select(a => a.Name).ToArray();
            var query = new ViewQuery("artists", "albums").Configure(cfg => cfg.Keys(keys));

            var response = SUT.RunQuery<Album[]>(query);

            response.Should().BeSuccessfulGet(artists.Select(a => a.Albums).ToArray());
        }

        [Test]
        public void When_StartKey_and_EndKey_are_specified_Then_matching_rows_are_returned()
        {
            var artists = Artists.Skip(2).Take(5).ToArray();
            var query = new ViewQuery("artists", "albums").Configure(cfg => cfg
                .StartKey(artists.First().Name)
                .EndKey(artists.Last().Name));

            var response = SUT.RunQuery<Album[]>(query);

            response.Should().BeSuccessfulGet(artists.Select(a => a.Albums).ToArray());
        }

        [Test]
        public void When_StartKey_and_EndKey_with_non_inclusive_end_are_specified_Then_matching_rows_are_returned()
        {
            var artists = Artists.Skip(2).Take(5).ToArray();
            var query = new ViewQuery("artists", "albums").Configure(cfg => cfg
                .StartKey(artists.First().Name)
                .EndKey(artists.Last().Name)
                .InclusiveEnd(false));

            var response = SUT.RunQuery<Album[]>(query);

            response.Should().BeSuccessfulGet(artists.Take(artists.Length - 1).Select(a => a.Albums).ToArray());
        }

        protected virtual Album[][] GetExpectedAlbums(Func<IEnumerable<Artist>,IEnumerable<Artist>> modifyArtists)
        {
            var expected = Artists.OrderBy(a => a.Name);

            return modifyArtists(expected).Select(a => a.Albums).ToArray();
        }
    }
}