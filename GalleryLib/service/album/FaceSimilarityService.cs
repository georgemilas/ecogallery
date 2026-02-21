
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;

namespace GalleryLib.service.album;
public class FaceSimilarityService
{
    private readonly FaceRepository _faceRepository;
    public FaceSimilarityService(DatabaseConfiguration dbConfig)        
    {
        _faceRepository = new FaceRepository(dbConfig);
    }

    /// <summary>
    /// Find the most similar face person to the given embedding using cosine similarity.
    /// Returns null if no match is found above the threshold.
    /// </summary>
    public async Task<(FacePerson? Person, float Similarity)> FindMostSimilarPersonAsync(
        float[] embedding, float threshold = 0.5f)
    {
        var persons = await _faceRepository.GetAllFacePersonsAsync();

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

}