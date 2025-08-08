import useHttp from "./useHttp";

export type RatingCategoryResponse = {
    id: number;
    name: string;
    minScore: number;
    maxScore: number;
    colorHex: string;
    displayOrder: number;
};

export type RatingResponse = {
    itemId: string;
    itemType: string;
    categoryId?: number;
    personalScore?: number;
    position?: number;
    albumId?: string;
    isNewRating: boolean;
};

export type ComparisonRequest = {
    itemId1: string;
    itemId2: string;
    itemType: string;
    winnerId: string;
};

export type ComparisonResultResponse = {
    success: boolean;
    message: string;
    updatedScore?: number;
    updatedPosition?: number;
};

const useRating = () => {
    const { getMany, post, del } = useHttp();
    const baseEndpoint = "api/rating";

    const getRatingCategories = async (): Promise<RatingCategoryResponse[]> => {
        return await getMany<RatingCategoryResponse>(`${baseEndpoint}/categories`);
    };

    const startRating = async (itemType: string, itemId: string): Promise<RatingResponse> => {
        return await post<RatingResponse, Record<string, never>>({}, `${baseEndpoint}/rate/${itemType}/${itemId}`);
    };

    const submitComparison = async (request: ComparisonRequest): Promise<ComparisonResultResponse> => {
        return await post<ComparisonResultResponse, ComparisonRequest>(request, `${baseEndpoint}/compare`);
    };

    const getUserRatings = async (itemType: string): Promise<RatingResponse[]> => {
        return await getMany<RatingResponse>(`${baseEndpoint}/user/${itemType}`);
    };

    const getRatingSuggestions = async (): Promise<any[]> => {
        return await getMany<any>(`${baseEndpoint}/suggestions`);
    };

    const saveRating = async (itemType: string, itemId: string, personalScore: number, categoryId: number): Promise<RatingResponse> => {
        return await post<RatingResponse, any>({
            itemId,
            itemType,
            personalScore,
            categoryId
        }, `${baseEndpoint}/save`);
    };
    
    const deleteRating = async (itemType: string, itemId: string): Promise<RatingResponse> => {
        return await del<RatingResponse>(`${baseEndpoint}/user/${itemType}/${itemId}`);
    };

    return { 
        getRatingCategories, 
        startRating, 
        submitComparison, 
        getUserRatings, 
        getRatingSuggestions,
        saveRating,
        deleteRating
    };
};

export default useRating;