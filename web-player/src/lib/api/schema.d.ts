

export interface paths {
    "/albums": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetAlbums"];
        put?: never;
        
        post: operations["CreateAlbum"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/albums/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetAlbumById"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/albums/users/{userId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetAlbumsByUserId"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/albums/recent": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetRecentlyListenedAlbums"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/albums/{albumId}/tracks": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetAlbumTracks"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/albums/{id}/cover": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetAlbumCover"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/albums/upload-picture": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        
        post: operations["RequestAlbumPictureUpload"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/albums/{albumId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        
        put: operations["UpdateAlbum"];
        post?: never;
        
        delete: operations["DeleteAlbum"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/albums/{albumId}/tracks/{trackId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        
        post: operations["AddTrackToAlbum"];
        
        delete: operations["RemoveTrackFromAlbum"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/config/playback": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetPlaybackPublicConfig"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/likes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetLikedTracks"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/likes/toggle": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        
        post: operations["ToggleTrackLike"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/mixes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetMixes"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/mixes/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetMixById"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/playlists": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetPlayLists"];
        put?: never;
        
        post: operations["CreatePlayList"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/playlists/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetPlayListById"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/playlists/users/{userId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetPlayListsByUserId"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/playlists/{playlistId}/tracks": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetPlayListTracks"];
        put?: never;
        
        post: operations["AddPlayListTrack"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/playlists/{id}/cover": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetPlayListCover"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/playlists/{playlistId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        
        put: operations["UpdatePlayList"];
        post?: never;
        
        delete: operations["DeletePlayList"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/playlists/upload-picture": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        
        post: operations["RequestPlayListPictureUpload"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/playlists/{playlistId}/tracks/{trackId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        
        delete: operations["RemoveTrackFromPlayList"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/tracks": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetTracks"];
        put?: never;
        
        post: operations["CreateTrack"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/tracks/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetTrackById"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/tracks/{id}/cover": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetTrackCover"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/tracks/{id}/stream": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetTrackStream"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/tracks/listens/{listenId}/progress": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put: operations["RecordListeningProgress"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/tracks/users/{userId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetTracksByUserId"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/tracks/{trackId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        
        delete: operations["DeleteTrack"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/tracks/upload-urls": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        
        post: operations["RequestTrackUploadUrls"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetUsers"];
        put?: never;
        
        post: operations["CreateUser"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetUserById"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/{id}/listening-history": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetListeningHistory"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/{id}/last-listened-track": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };

        get: operations["GetLastTrackListenedByUserId"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/{id}/listening-stats": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };

        get: operations["GetListeningStats"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/{id}/profile": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["GetUserProfile"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/{id}/is-following": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        
        get: operations["IsFollowingUser"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/users/{id}/follow": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        
        post: operations["FollowUser"];
        
        delete: operations["UnfollowUser"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
}
export type webhooks = Record<string, never>;
export interface components {
    schemas: {
        AddPlayListTrackRequest: {
            
            trackId: string;
        };
        AlbumApplicationResponse: {
            
            id: string;
            title: string;
            description: null | string;
            
            releaseYear: null | number | string;
            ownerUserId: string;
            
            trackCount: number | string;
            smallImageKeyName: null | string;
            mediumImageKeyName: null | string;
            largeImageKeyName: null | string;
            
            createdAt: string;
            
            updatedAt: string;
            coverTrackIds: string[];
        };
        AlbumPictureUploadResponse: {
            
            intentId: string;
            bucket: string;
            key: string;
            pictureName: string;
            contentType: string;
            
            expiresInSeconds: number | string;
            uploadUrl: string;
        };
        AlbumSearchItemResponse: {
            album: components["schemas"]["AlbumApplicationResponse"];
        };
        AlbumsSearchResponse: {
            items: components["schemas"]["AlbumSearchItemResponse"][];
            
            pageNumber: number | string;
            
            pageSize: number | string;
            
            pageCount: number | string;
            
            totalItemCount: number | string;
            hasPreviousPage: boolean;
            hasNextPage: boolean;
        };
        CreateAlbumRequest: {
            title: string;
            description: null | string;
            
            releaseYear: null | number | string;
            
            pictureIntentId: string;
        };
        CreatePlayListRequest: {
            name: string;
            description: null | string;
            
            pictureIntentId: null | string;
            visibility?: components["schemas"]["PlaylistVisibility"];
        };
        CreateTrackRequest: {
            title: string;
            
            pictureIntentId: string;
            
            audioIntentId: string;
            tags: components["schemas"]["Genre"][];
        };
        Genre: "Pop" | "Rock" | "HipHop" | "RnB" | "Jazz" | "Blues" | "Classical" | "Electronic" | "House" | "Techno" | "Trance" | "Dubstep" | "DrumAndBass" | "Metal" | "Punk" | "Reggae" | "Reggaeton" | "Country" | "Folk" | "Indie" | "KPop" | "Latin" | "Soul" | "Funk" | "Ambient" | "Lofi";
        CreateUserRequest: {
            
            id: number | string;
            name: string;
            firstName: null | string;
            secondName: null | string;
        };
        HttpValidationProblemDetails: {
            type?: null | string;
            title?: null | string;
            
            status?: null | number | string;
            detail?: null | string;
            instance?: null | string;
            errors?: {
                [key: string]: string[];
            };
        };
        ListeningStatsResponse: {
            
            tracksThisWeek: number | string;
            
            secondsThisWeek: number | string;
            
            streakDays: number | string;
        };
        MixApplicationResponse: {
            
            id: string;
            title: string;
            subtitle: null | string;
            
            itemCount: number | string;
            items: components["schemas"]["MixItemApplicationResponse"][];
        };
        MixItemApplicationResponse: {
            
            trackId: string;
            title: string;
            artist: null | string;
            
            durationSeconds: number | string;
            
            listensCount: number | string;
        };
        PaginatedResponseOfAlbumApplicationResponse: {
            items: components["schemas"]["AlbumApplicationResponse"][];
            
            pageNumber: number | string;
            
            pageSize: number | string;
            
            pageCount: number | string;
            
            totalItemCount: number | string;
            hasNextPage: boolean;
            hasPreviousPage: boolean;
        };
        PaginatedResponseOfPlayListApplicationResponse: {
            items: components["schemas"]["PlayListApplicationResponse"][];
            
            pageNumber: number | string;
            
            pageSize: number | string;
            
            pageCount: number | string;
            
            totalItemCount: number | string;
            hasNextPage: boolean;
            hasPreviousPage: boolean;
        };
        PaginatedResponseOfTrackApplicationResponse: {
            items: components["schemas"]["TrackApplicationResponse"][];
            
            pageNumber: number | string;
            
            pageSize: number | string;
            
            pageCount: number | string;
            
            totalItemCount: number | string;
            hasNextPage: boolean;
            hasPreviousPage: boolean;
        };
        PaginatedResponseOfUserApplicationResponse: {
            items: components["schemas"]["UserApplicationResponse"][];
            
            pageNumber: number | string;
            
            pageSize: number | string;
            
            pageCount: number | string;
            
            totalItemCount: number | string;
            hasNextPage: boolean;
            hasPreviousPage: boolean;
        };
        PlaybackPublicConfigResponse: {
            allowAnonymousListening: boolean;
            
            anonymousFragmentSeconds: number | string;
        };
        PlayListApplicationResponse: {
            
            id: string;
            name: string;
            description: null | string;
            smallImageKeyName: null | string;
            mediumImageKeyName: null | string;
            largeImageKeyName: null | string;
            visibility: components["schemas"]["PlaylistVisibility"];
            
            createdAt: string;
            
            updatedAt: string;
            coverTrackIds: string[];
        };
        PlayListPictureUploadResponse: {
            
            intentId: string;
            bucket: string;
            key: string;
            pictureName: string;
            contentType: string;
            
            expiresInSeconds: number | string;
            uploadUrl: string;
        };
        
        PlaylistVisibility: "Private" | "Public";
        
        ProcessingStatus: "Pending" | "Processing" | "Completed" | "Failed";
        RequestAlbumPictureUploadRequest: {
            fileType: string;
            contentType: string;
            
            expectedSizeBytes?: null | number | string;
        };
        RequestPlayListPictureUploadRequest: {
            fileType: string;
            contentType: string;
            
            expectedSizeBytes?: null | number | string;
        };
        RecordListeningProgressRequest: {
            playedSeconds: number | string;
        };
        RequestTrackUploadUrlsRequest: {
            pictureFileType: string;
            pictureContentType: string;
            audioFileType: string;
            audioContentType: string;
            
            expectedPictureSizeBytes?: null | number | string;
            
            expectedAudioSizeBytes?: null | number | string;
        };
        ToggleLikeRequest: {
            
            trackId: string;
        };
        TrackApplicationResponse: {
            
            id: string;
            title: string;
            artist: null | string;
            audioStatus: components["schemas"]["ProcessingStatus"];
            
            duration: number | string;
            
            listensCount: number | string;
            
            createdAt: string;
            
            updatedAt: string;
            ownerUserId: string;
        };
        TrackSearchItemResponse: {
            track: components["schemas"]["TrackApplicationResponse"];
        };
        TracksSearchResponse: {
            items: components["schemas"]["TrackSearchItemResponse"][];
            
            pageNumber: number | string;
            
            pageSize: number | string;
            
            pageCount: number | string;
            
            totalItemCount: number | string;
            hasPreviousPage: boolean;
            hasNextPage: boolean;
        };
        TrackStreamResponse: {
            manifestUrl: string;
            ticket: string;
            
            expiresInSeconds: number | string;
            /** Format: uuid */
            listenId?: string | null;
        };
        TrackUploadUrlsResponse: {
            
            pictureIntentId: string;
            
            audioIntentId: string;
            bucket: string;
            pictureKey: string;
            pictureName: string;
            pictureContentType: string;
            pictureUploadUrl: string;
            audioKey: string;
            audioName: string;
            audioContentType: string;
            audioUploadUrl: string;
            
            expiresInSeconds: number | string;
        };
        UpdateAlbumRequest: {
            newTitle: string;
            newDescription: null | string;
            
            newReleaseYear: null | number | string;
            
            newPictureIntentId: null | string;
        };
        UpdatePlayListRequest: {
            newName: null | string;
            newDescription: null | string;
            
            newPictureIntentId: null | string;
            newVisibility?: null | components["schemas"]["PlaylistVisibility"];
        };
        UserApplicationResponse: {
            id: string;
            name: string;
            firstName: null | string;
            secondName: null | string;
            profilePictureUrl: null | string;
            
            createdAt: string;
            
            updatedAt: string;
        };
        UserProfileResponse: {
            id: string;
            name: string;
            firstName: null | string;
            secondName: null | string;
            profilePictureUrl: null | string;
            
            createdAt: string;
            
            followersCount: number | string;
            
            followingCount: number | string;
        };
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    GetAlbums: {
        parameters: {
            query?: {
                title?: string;
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["AlbumsSearchResponse"];
                };
            };
        };
    };
    CreateAlbum: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["CreateAlbumRequest"];
            };
        };
        responses: {
            
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["AlbumApplicationResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetAlbumById: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["AlbumApplicationResponse"];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetAlbumsByUserId: {
        parameters: {
            query?: {
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path: {
                userId: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginatedResponseOfAlbumApplicationResponse"];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetRecentlyListenedAlbums: {
        parameters: {
            query?: {
                limit?: number | string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["AlbumApplicationResponse"][];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetAlbumTracks: {
        parameters: {
            query?: {
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path: {
                albumId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginatedResponseOfTrackApplicationResponse"];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetAlbumCover: {
        parameters: {
            query?: {
                size?: string;
            };
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    RequestAlbumPictureUpload: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["RequestAlbumPictureUploadRequest"];
            };
        };
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["AlbumPictureUploadResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    UpdateAlbum: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                albumId: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["UpdateAlbumRequest"];
            };
        };
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["AlbumApplicationResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    DeleteAlbum: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                albumId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    AddTrackToAlbum: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                albumId: string;
                trackId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    RemoveTrackFromAlbum: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                albumId: string;
                trackId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetPlaybackPublicConfig: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlaybackPublicConfigResponse"];
                };
            };
        };
    };
    GetLikedTracks: {
        parameters: {
            query?: {
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginatedResponseOfTrackApplicationResponse"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    ToggleTrackLike: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["ToggleLikeRequest"];
            };
        };
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": boolean;
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetMixes: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["MixApplicationResponse"][];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetMixById: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["MixApplicationResponse"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetPlayLists: {
        parameters: {
            query?: {
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginatedResponseOfPlayListApplicationResponse"];
                };
            };
        };
    };
    CreatePlayList: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["CreatePlayListRequest"];
            };
        };
        responses: {
            
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlayListApplicationResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetPlayListById: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlayListApplicationResponse"];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetPlayListsByUserId: {
        parameters: {
            query?: {
                name?: string;
                pageNumber?: number | string;
                pageSize?: number | string;
                onlyPublic?: boolean;
            };
            header?: never;
            path: {
                userId: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginatedResponseOfPlayListApplicationResponse"];
                };
            };
        };
    };
    GetPlayListTracks: {
        parameters: {
            query?: {
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path: {
                playlistId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginatedResponseOfTrackApplicationResponse"];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    AddPlayListTrack: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                playlistId: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["AddPlayListTrackRequest"];
            };
        };
        responses: {
            
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["TrackApplicationResponse"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetPlayListCover: {
        parameters: {
            query?: {
                size?: string;
            };
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    UpdatePlayList: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                playlistId: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["UpdatePlayListRequest"];
            };
        };
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlayListApplicationResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    DeletePlayList: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                playlistId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    RequestPlayListPictureUpload: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["RequestPlayListPictureUploadRequest"];
            };
        };
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlayListPictureUploadResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    RemoveTrackFromPlayList: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                playlistId: string;
                trackId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetTracks: {
        parameters: {
            query?: {
                name?: string;
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["TracksSearchResponse"];
                };
            };
        };
    };
    CreateTrack: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["CreateTrackRequest"];
            };
        };
        responses: {
            
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["TrackApplicationResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetTrackById: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["TrackApplicationResponse"];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetTrackCover: {
        parameters: {
            query?: {
                size?: string;
            };
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetTrackStream: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["TrackStreamResponse"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetTracksByUserId: {
        parameters: {
            query?: {
                name?: string;
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path: {
                userId: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginatedResponseOfTrackApplicationResponse"];
                };
            };
        };
    };
    DeleteTrack: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                trackId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    RecordListeningProgress: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                listenId: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["RecordListeningProgressRequest"];
            };
        };
        responses: {
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    RequestTrackUploadUrls: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["RequestTrackUploadUrlsRequest"];
            };
        };
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["TrackUploadUrlsResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetUsers: {
        parameters: {
            query?: {
                usernameSearch?: string;
                pageNumber?: number | string;
                pageSize?: number | string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginatedResponseOfUserApplicationResponse"];
                };
            };
        };
    };
    CreateUser: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["CreateUserRequest"];
            };
        };
        responses: {
            
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["UserApplicationResponse"];
                };
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetUserById: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["UserApplicationResponse"];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetListeningHistory: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["TrackApplicationResponse"][];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetListeningStats: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {

            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ListeningStatsResponse"];
                };
            };
        };
    };
    GetLastTrackListenedByUserId: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {

            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["TrackApplicationResponse"];
                };
            };

            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetUserProfile: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["UserProfileResponse"];
                };
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    IsFollowingUser: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": boolean;
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    FollowUser: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
                };
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    UnfollowUser: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: number | string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
}
