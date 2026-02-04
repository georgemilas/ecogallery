using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.service.database;

namespace GalleryLib.repository;

public class FaceRepository(DatabaseConfiguration dbConfig) : IDisposable, IAsyncDisposable
{
    private readonly PostgresDatabaseService _db = new(dbConfig.ToConnectionString());

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #region FaceEmbedding Operations

    public async Task<long> InsertFaceEmbeddingAsync(FaceEmbedding face)
    {
        var sql = @"
            INSERT INTO public.face_embedding
                (album_image_id, face_person_id, embedding, bounding_box_x, bounding_box_y,
                 bounding_box_width, bounding_box_height, confidence, is_confirmed,
                 created_utc, last_updated_utc)
            VALUES
                (@album_image_id, @face_person_id, @embedding, @bounding_box_x, @bounding_box_y,
                 @bounding_box_width, @bounding_box_height, @confidence, @is_confirmed,
                 @created_utc, @last_updated_utc)
            RETURNING id";

        var result = await _db.ExecuteScalarAsync<long>(sql, new
        {
            album_image_id = face.AlbumImageId,
            face_person_id = face.FacePersonId,
            embedding = face.Embedding,
            bounding_box_x = face.BoundingBoxX,
            bounding_box_y = face.BoundingBoxY,
            bounding_box_width = face.BoundingBoxWidth,
            bounding_box_height = face.BoundingBoxHeight,
            confidence = face.Confidence,
            is_confirmed = face.IsConfirmed,
            created_utc = DateTimeOffset.UtcNow,
            last_updated_utc = DateTimeOffset.UtcNow
        });

        return result;
    }

    public async Task<List<FaceEmbedding>> GetFaceEmbeddingsByImageIdAsync(long albumImageId)
    {
        var sql = @"
            SELECT id, album_image_id, face_person_id, embedding, bounding_box_x, bounding_box_y,
                   bounding_box_width, bounding_box_height, confidence, is_confirmed,
                   created_utc, last_updated_utc
            FROM public.face_embedding
            WHERE album_image_id = @album_image_id";

        return await _db.QueryAsync(sql, FaceEmbedding.CreateFromDataReader, new { album_image_id = albumImageId });
    }

    public async Task<List<FaceEmbedding>> GetAllFaceEmbeddingsAsync(int limit = 10000)
    {
        var sql = @"
            SELECT id, album_image_id, face_person_id, embedding, bounding_box_x, bounding_box_y,
                   bounding_box_width, bounding_box_height, confidence, is_confirmed,
                   created_utc, last_updated_utc
            FROM public.face_embedding
            ORDER BY id
            LIMIT @limit";

        return await _db.QueryAsync(sql, FaceEmbedding.CreateFromDataReader, new { limit });
    }

    public async Task<List<FaceEmbedding>> GetUnclusteredFaceEmbeddingsAsync(int limit = 1000)
    {
        var sql = @"
            SELECT id, album_image_id, face_person_id, embedding, bounding_box_x, bounding_box_y,
                   bounding_box_width, bounding_box_height, confidence, is_confirmed,
                   created_utc, last_updated_utc
            FROM public.face_embedding
            WHERE face_person_id IS NULL
            ORDER BY id
            LIMIT @limit";

        return await _db.QueryAsync(sql, FaceEmbedding.CreateFromDataReader, new { limit });
    }

    public async Task UpdateFacePersonIdAsync(long faceEmbeddingId, long facePersonId)
    {
        var sql = @"
            UPDATE public.face_embedding
            SET face_person_id = @face_person_id, last_updated_utc = @last_updated_utc
            WHERE id = @id";

        await _db.ExecuteAsync(sql, new
        {
            id = faceEmbeddingId,
            face_person_id = facePersonId,
            last_updated_utc = DateTimeOffset.UtcNow
        });
    }

    public async Task<bool> HasFaceEmbeddingsForImageAsync(long albumImageId)
    {
        var sql = "SELECT EXISTS(SELECT 1 FROM public.face_embedding WHERE album_image_id = @album_image_id)";
        var result = await _db.ExecuteScalarAsync<bool>(sql, new { album_image_id = albumImageId });
        return result;
    }

    public async Task<FaceEmbedding?> GetFaceEmbeddingByIdAsync(long faceId)
    {
        var sql = @"
            SELECT id, album_image_id, face_person_id, embedding, bounding_box_x, bounding_box_y,
                   bounding_box_width, bounding_box_height, confidence, is_confirmed,
                   created_utc, last_updated_utc
            FROM public.face_embedding
            WHERE id = @id";

        var results = await _db.QueryAsync(sql, FaceEmbedding.CreateFromDataReader, new { id = faceId });
        return results.FirstOrDefault();
    }

    public async Task<int> DeleteFaceEmbeddingByIdAsync(long faceId)
    {
        var sql = @"DELETE FROM public.face_embedding WHERE id = @id";              
        var results = await _db.ExecuteAsync(sql, new { id = faceId });
        return results;
    }

    #endregion

    #region FacePerson Operations

    public async Task<long> InsertFacePersonAsync(FacePerson person)
    {
        var sql = @"
            INSERT INTO public.face_person
                (name, representative_embedding, face_count, created_utc, last_updated_utc)
            VALUES
                (@name, @representative_embedding, @face_count, @created_utc, @last_updated_utc)
            RETURNING id";

        var result = await _db.ExecuteScalarAsync<long>(sql, new
        {
            name = person.Name,
            representative_embedding = person.RepresentativeEmbedding,
            face_count = person.FaceCount,
            created_utc = DateTimeOffset.UtcNow,
            last_updated_utc = DateTimeOffset.UtcNow
        });

        return result;
    }

    public async Task<List<FacePerson>> GetAllFacePersonsAsync()
    {
        var sql = @"
            SELECT id, name, representative_embedding, face_count, created_utc, last_updated_utc
            FROM public.face_person
            ORDER BY face_count DESC";

        return await _db.QueryAsync(sql, FacePerson.CreateFromDataReader);
    }

    public async Task<FacePerson?> GetFacePersonByIdAsync(long id)
    {
        var sql = @"
            SELECT id, name, representative_embedding, face_count, created_utc, last_updated_utc
            FROM public.face_person
            WHERE id = @id";

        var results = await _db.QueryAsync(sql, FacePerson.CreateFromDataReader, new { id });
        return results.FirstOrDefault();
    }

    public async Task<int> DeleteFacePersonByIdAsync(long id)
    {
        var sql = @"DELETE FROM public.face_person WHERE id = @id";
            
        var results = await _db.ExecuteAsync(sql, new { id });
        return results;
    }

    public async Task UpdateFacePersonAsync(FacePerson person)
    {
        var sql = @"
            UPDATE public.face_person
            SET name = @name, representative_embedding = @representative_embedding,
                face_count = @face_count, last_updated_utc = @last_updated_utc
            WHERE id = @id";

        await _db.ExecuteAsync(sql, new
        {
            id = person.Id,
            name = person.Name,
            representative_embedding = person.RepresentativeEmbedding,
            face_count = person.FaceCount,
            last_updated_utc = DateTimeOffset.UtcNow
        });
    }

    public async Task IncrementFaceCountAsync(long facePersonId)
    {
        var sql = @"
            UPDATE public.face_person
            SET face_count = face_count + 1, last_updated_utc = @last_updated_utc
            WHERE id = @id";

        await _db.ExecuteAsync(sql, new
        {
            id = facePersonId,
            last_updated_utc = DateTimeOffset.UtcNow
        });
    }

    #endregion

    #region Similarity Search

    /// <summary>
    /// Find the most similar face person to the given embedding using cosine similarity.
    /// Returns null if no match is found above the threshold.
    /// </summary>
    public async Task<(FacePerson? Person, float Similarity)> FindMostSimilarPersonAsync(
        float[] embedding, float threshold = 0.5f)
    {
        var persons = await GetAllFacePersonsAsync();

        FacePerson? bestMatch = null;
        float bestSimilarity = threshold;

        foreach (var person in persons)
        {
            if (person.RepresentativeEmbedding == null) continue;

            var similarity = CosineSimilarity(embedding, person.RepresentativeEmbedding);
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = person;
            }
        }

        return (bestMatch, bestSimilarity);
    }

    /// <summary>
    /// Calculate cosine similarity between two embedding vectors.
    /// Returns value between -1 and 1, where 1 means identical.
    /// </summary>
    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Embedding vectors must have the same length");

        float dotProduct = 0;
        float normA = 0;
        float normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0) return 0;

        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    /// <summary>
    /// Calculate cosine distance (1 - similarity) between two embeddings.
    /// Returns value between 0 and 2, where 0 means identical.
    /// </summary>
    public static float CosineDistance(float[] a, float[] b)
    {
        return 1 - CosineSimilarity(a, b);
    }

    /// <summary>
    /// Calculate average embedding from multiple embeddings.
    /// Used for updating representative embedding of a person.
    /// </summary>
    public static float[] AverageEmbedding(IEnumerable<float[]> embeddings)
    {
        var list = embeddings.ToList();
        if (list.Count == 0) return [];

        var length = list[0].Length;
        var result = new float[length];

        foreach (var embedding in list)
        {
            for (int i = 0; i < length; i++)
            {
                result[i] += embedding[i];
            }
        }

        for (int i = 0; i < length; i++)
        {
            result[i] /= list.Count;
        }

        // Normalize the result
        float norm = 0;
        for (int i = 0; i < length; i++)
        {
            norm += result[i] * result[i];
        }
        norm = MathF.Sqrt(norm);

        if (norm > 0)
        {
            for (int i = 0; i < length; i++)
            {
                result[i] /= norm;
            }
        }

        return result;
    }

    #endregion

    #region Search by Person

    /// <summary>
    /// Get all album image IDs that contain faces of a specific person (by person ID).
    /// </summary>
    public async Task<List<long>> GetImageIdsByPersonIdAsync(long personId)
    {
        var sql = @"
            SELECT DISTINCT album_image_id
            FROM public.face_embedding
            WHERE face_person_id = @person_id
            ORDER BY album_image_id";

        return await _db.QueryAsync(sql, reader => reader.GetInt64(0), new { person_id = personId });
    }

    /// <summary>
    /// Get all album image IDs that contain faces of persons with the given name.
    /// This allows finding images across multiple person IDs that share the same name.
    /// </summary>
    public async Task<List<long>> GetImageIdsByPersonNameAsync(string personName)
    {
        var sql = @"
            SELECT DISTINCT fe.album_image_id
            FROM public.face_embedding fe
            JOIN public.face_person fp ON fe.face_person_id = fp.id
            WHERE fp.name = @name
            ORDER BY fe.album_image_id";

        return await _db.QueryAsync(sql, reader => reader.GetInt64(0), new { name = personName });
    }

    /// <summary>
    /// Get all persons with the given name (case-sensitive).
    /// </summary>
    public async Task<List<FacePerson>> GetPersonsByNameAsync(string name)
    {
        var sql = @"
            SELECT id, name, representative_embedding, face_count, created_utc, last_updated_utc
            FROM public.face_person
            WHERE name = @name
            ORDER BY face_count DESC";

        return await _db.QueryAsync(sql, FacePerson.CreateFromDataReader, new { name });
    }

    /// <summary>
    /// Get top N named persons ordered by distinct image count descending.
    /// Groups by name (not person ID) so same-named persons are combined.
    /// Includes a representative image path and face bounding box for thumbnail display.
    /// </summary>
    public async Task<List<PersonWithImageCount>> GetTopNamedPersonsAsync(int limit = 20)
    {
        var sql = @"
            WITH person_counts AS (
                SELECT fp.name, COUNT(DISTINCT (COALESCE(ai.image_sha256, ai.image_path))) as image_count
                FROM public.face_person fp
                JOIN public.face_embedding fe ON fe.face_person_id = fp.id
                JOIN album_image ai ON ai.id = fe.album_image_id
                WHERE fp.name IS NOT NULL AND fp.name != ''
                GROUP BY fp.name
                ORDER BY image_count DESC
                LIMIT @limit
            ),
            person_faces AS (
                SELECT DISTINCT ON (fp.name)
                    fp.name,
                    ai.image_path as thumbnail_path,
                    ai.image_width,
                    ai.image_height,
                    fe.bounding_box_x,
                    fe.bounding_box_y,
                    fe.bounding_box_width,
                    fe.bounding_box_height
                FROM public.face_person fp
                JOIN public.face_embedding fe ON fe.face_person_id = fp.id
                JOIN public.album_image ai ON ai.id = fe.album_image_id
                JOIN public.image_metadata exif ON ai.id = exif.album_image_id
                WHERE fp.name IN (SELECT name FROM person_counts)
                ORDER BY fp.name, coalesce(exif.date_taken, ai.image_timestamp_utc) DESC
            )
            SELECT
                pc.name,
                pc.image_count,
                pf.thumbnail_path,
                pf.image_width,
                pf.image_height,
                pf.bounding_box_x,
                pf.bounding_box_y,
                pf.bounding_box_width,
                pf.bounding_box_height
            FROM person_counts pc
            LEFT JOIN person_faces pf ON pc.name = pf.name
            ORDER BY pc.image_count DESC";

        return await _db.QueryAsync(sql, reader => new PersonWithImageCount
        {
            Name = reader.GetString(reader.GetOrdinal("name")),
            ImageCount = reader.GetInt32(reader.GetOrdinal("image_count")),
            ThumbnailPath = reader.IsDBNull(reader.GetOrdinal("thumbnail_path")) ? null : reader.GetString(reader.GetOrdinal("thumbnail_path")),
            ImageWidth = reader.IsDBNull(reader.GetOrdinal("image_width")) ? null : reader.GetInt32(reader.GetOrdinal("image_width")),
            ImageHeight = reader.IsDBNull(reader.GetOrdinal("image_height")) ? null : reader.GetInt32(reader.GetOrdinal("image_height")),
            BoundingBoxX = reader.IsDBNull(reader.GetOrdinal("bounding_box_x")) ? null : reader.GetFloat(reader.GetOrdinal("bounding_box_x")),
            BoundingBoxY = reader.IsDBNull(reader.GetOrdinal("bounding_box_y")) ? null : reader.GetFloat(reader.GetOrdinal("bounding_box_y")),
            BoundingBoxWidth = reader.IsDBNull(reader.GetOrdinal("bounding_box_width")) ? null : reader.GetFloat(reader.GetOrdinal("bounding_box_width")),
            BoundingBoxHeight = reader.IsDBNull(reader.GetOrdinal("bounding_box_height")) ? null : reader.GetFloat(reader.GetOrdinal("bounding_box_height"))
        }, new { limit });
    }

    #endregion
}

public record PersonWithImageCount
{
    public required string Name { get; init; }
    public int ImageCount { get; init; }
    public string? ThumbnailPath { get; init; }
    public int? ImageWidth { get; init; }
    public int? ImageHeight { get; init; }
    public float? BoundingBoxX { get; init; }
    public float? BoundingBoxY { get; init; }
    public float? BoundingBoxWidth { get; init; }
    public float? BoundingBoxHeight { get; init; }
}
