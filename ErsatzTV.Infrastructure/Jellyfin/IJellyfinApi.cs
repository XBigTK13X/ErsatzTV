﻿using ErsatzTV.Infrastructure.Jellyfin.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Jellyfin;

[Headers("Accept: application/json")]
public interface IJellyfinApi
{
    [Get("/System/Info/Public")]
    public Task<JellyfinSystemInformationResponse> GetSystemInformation(
        CancellationToken cancellationToken);


    [Get("/Users")]
    public Task<List<JellyfinUserResponse>> GetUsers();

    [Get("/Library/VirtualFolders")]
    public Task<List<JellyfinLibraryResponse>> GetLibraries();

    [Get("/Items")]
    public Task<JellyfinLibraryItemsResponse> GetLibraryStats(
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string includeItemTypes,
        [Query]
        bool recursive = true,
        [Query]
        string filters = "IsNotFolder",
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetMovieLibraryItems(
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People,OfficialRating,ProviderIds,Chapters",
        [Query]
        string includeItemTypes = "Movie",
        [Query]
        bool recursive = true,
        [Query]
        string filters = "IsNotFolder",
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetShowLibraryItems(
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People,OfficialRating,ProviderIds",
        [Query]
        string includeItemTypes = "Series",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetSeasonLibraryItems(
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields = "Path,DateCreated,Etag,Taglines,ProviderIds",
        [Query]
        string includeItemTypes = "Season",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetEpisodeLibraryItems(
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields = "Path,Genres,Tags,DateCreated,Etag,Overview,ProviderIds,People,Chapters",
        [Query]
        string includeItemTypes = "Episode",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetCollectionLibraryItems(
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields = "Etag",
        [Query]
        string includeItemTypes = "BoxSet",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetCollectionItems(
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields = "Etag",
        [Query]
        string includeItemTypes = "Movie,Series,Season,Episode",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items/{itemId}/PlaybackInfo")]
    public Task<JellyfinPlaybackInfoResponse> GetPlaybackInfo(
        [Query]
        string userId,
        string itemId);
}
