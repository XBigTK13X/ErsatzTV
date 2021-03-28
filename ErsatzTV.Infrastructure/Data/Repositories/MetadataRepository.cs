﻿using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MetadataRepository : IMetadataRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MetadataRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<bool> Update(Metadata metadata)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            dbContext.Entry(metadata).State = EntityState.Modified;
            return await dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> Add(Metadata metadata)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            dbContext.Entry(metadata).State = EntityState.Added;
            foreach (Genre genre in metadata.Genres)
            {
                dbContext.Entry(genre).State = EntityState.Added;
            }

            foreach (Tag tag in metadata.Tags)
            {
                dbContext.Entry(tag).State = EntityState.Added;
            }

            return await dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateLocalStatistics(
            int mediaVersionId,
            MediaVersion incoming,
            bool updateVersion = true)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Option<MediaVersion> maybeVersion = await dbContext.MediaVersions
                .Include(v => v.Streams)
                .OrderBy(v => v.Id)
                .SingleOrDefaultAsync(v => v.Id == mediaVersionId)
                .Map(Optional);

            return await maybeVersion.Match(
                async existing =>
                {
                    if (updateVersion)
                    {
                        existing.DateUpdated = incoming.DateUpdated;
                        existing.Duration = incoming.Duration;
                        existing.SampleAspectRatio = incoming.SampleAspectRatio;
                        existing.DisplayAspectRatio = incoming.DisplayAspectRatio;
                        existing.Width = incoming.Width;
                        existing.Height = incoming.Height;
                        existing.VideoScanKind = incoming.VideoScanKind;
                    }

                    var toAdd = incoming.Streams.Filter(s => existing.Streams.All(es => es.Index != s.Index)).ToList();
                    var toRemove = existing.Streams.Filter(es => incoming.Streams.All(s => s.Index != es.Index))
                        .ToList();
                    var toUpdate = incoming.Streams.Except(toAdd).ToList();

                    // add
                    existing.Streams.AddRange(toAdd);

                    // remove
                    existing.Streams.RemoveAll(s => toRemove.Contains(s));

                    // update
                    foreach (MediaStream incomingStream in toUpdate)
                    {
                        MediaStream existingStream = existing.Streams.First(s => s.Index == incomingStream.Index);

                        existingStream.Codec = incomingStream.Codec;
                        existingStream.Profile = incomingStream.Profile;
                        existingStream.MediaStreamKind = incomingStream.MediaStreamKind;
                        existingStream.Language = incomingStream.Language;
                        existingStream.Channels = incomingStream.Channels;
                        existingStream.Title = incomingStream.Title;
                        existingStream.Default = incomingStream.Default;
                        existingStream.Forced = incomingStream.Forced;
                    }

                    return await dbContext.SaveChangesAsync() > 0;
                },
                () => Task.FromResult(false));
        }

        public async Task<bool> UpdatePlexStatistics(int mediaVersionId, MediaVersion incoming)
        {
            bool updatedVersion = await _dbConnection.ExecuteAsync(
                @"UPDATE MediaVersion SET
                  SampleAspectRatio = @SampleAspectRatio,
                  VideoScanKind = @VideoScanKind,
                  DateUpdated = @DateUpdated
                  WHERE Id = @MediaVersionId",
                new
                {
                    incoming.SampleAspectRatio,
                    incoming.VideoScanKind,
                    incoming.DateUpdated,
                    MediaVersionId = mediaVersionId
                }).Map(result => result > 0);

            return await UpdateLocalStatistics(mediaVersionId, incoming, false) || updatedVersion;
        }

        public Task<Unit> UpdateArtworkPath(Artwork artwork) =>
            _dbConnection.ExecuteAsync(
                "UPDATE Artwork SET Path = @Path, DateUpdated = @DateUpdated WHERE Id = @Id",
                new { artwork.Path, artwork.DateUpdated, artwork.Id }).ToUnit();

        public Task<Unit> AddArtwork(Metadata metadata, Artwork artwork)
        {
            var parameters = new
            {
                artwork.ArtworkKind, metadata.Id, artwork.DateAdded, artwork.DateUpdated, artwork.Path
            };

            return metadata switch
            {
                MovieMetadata => _dbConnection.ExecuteAsync(
                        @"INSERT INTO Artwork (ArtworkKind, MovieMetadataId, DateAdded, DateUpdated, Path)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path)",
                        parameters)
                    .ToUnit(),
                ShowMetadata => _dbConnection.ExecuteAsync(
                        @"INSERT INTO Artwork (ArtworkKind, ShowMetadataId, DateAdded, DateUpdated, Path)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path)",
                        parameters)
                    .ToUnit(),
                SeasonMetadata => _dbConnection.ExecuteAsync(
                        @"INSERT INTO Artwork (ArtworkKind, SeasonMetadataId, DateAdded, DateUpdated, Path)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path)",
                        parameters)
                    .ToUnit(),
                EpisodeMetadata => _dbConnection.ExecuteAsync(
                        @"INSERT INTO Artwork (ArtworkKind, EpisodeMetadataId, DateAdded, DateUpdated, Path)
                            VALUES (@ArtworkKind, @Id, @DateAdded, @DateUpdated, @Path)",
                        parameters)
                    .ToUnit(),
                _ => Task.FromResult(Unit.Default)
            };
        }

        public Task<Unit> RemoveArtwork(Metadata metadata, ArtworkKind artworkKind) =>
            _dbConnection.ExecuteAsync(
                @"DELETE FROM Artwork WHERE ArtworkKind = @ArtworkKind AND (MovieMetadataId = @Id
                OR ShowMetadataId = @Id OR SeasonMetadataId = @Id OR EpisodeMetadataId = @Id)",
                new { ArtworkKind = artworkKind, metadata.Id }).ToUnit();

        public Task<Unit> MarkAsUpdated(ShowMetadata metadata, DateTime dateUpdated) =>
            _dbConnection.ExecuteAsync(
                @"UPDATE ShowMetadata SET DateUpdated = @DateUpdated WHERE Id = @Id",
                new { DateUpdated = dateUpdated, metadata.Id }).ToUnit();

        public Task<Unit> MarkAsUpdated(SeasonMetadata metadata, DateTime dateUpdated) =>
            _dbConnection.ExecuteAsync(
                @"UPDATE SeasonMetadata SET DateUpdated = @DateUpdated WHERE Id = @Id",
                new { DateUpdated = dateUpdated, metadata.Id }).ToUnit();

        public Task<Unit> MarkAsUpdated(MovieMetadata metadata, DateTime dateUpdated) =>
            _dbConnection.ExecuteAsync(
                @"UPDATE MovieMetadata SET DateUpdated = @DateUpdated WHERE Id = @Id",
                new { DateUpdated = dateUpdated, metadata.Id }).ToUnit();

        public Task<bool> RemoveGenre(Genre genre) =>
            _dbConnection.ExecuteAsync("DELETE FROM Genre WHERE Id = @GenreId", new { GenreId = genre.Id })
                .Map(result => result > 0);

        public Task<bool> RemoveTag(Tag tag) =>
            _dbConnection.ExecuteAsync("DELETE FROM Tag WHERE Id = @TagId", new { TagId = tag.Id })
                .Map(result => result > 0);

        public Task<bool> RemoveStudio(Studio studio) =>
            _dbConnection.ExecuteAsync("DELETE FROM Studio WHERE Id = @StudioId", new { StudioId = studio.Id })
                .Map(result => result > 0);
    }
}
