/* eslint-disable no-unused-expressions */
import axios, { AxiosError } from 'axios'
import { useAuth0 } from '@auth0/auth0-react'

const useHttp = () => {
	const { getAccessTokenSilently } = useAuth0();
    const apiUri = import.meta.env.VITE_API_ADDRESS;

	// Helper function to add retry logic for transient errors
	const retryRequest = async <T>(requestFn: () => Promise<T>, maxRetries = 2): Promise<T> => {
		for (let attempt = 0; attempt <= maxRetries; attempt++) {
			try {
				return await requestFn();
			} catch (error) {
				const axiosError = error as AxiosError;
				
				// Only retry on 500 errors and if we have retries left
				if (axiosError.response?.status === 500 && attempt < maxRetries) {
					// Exponential backoff: wait 500ms, then 1000ms
					const delay = 500 * Math.pow(2, attempt);
					await new Promise(resolve => setTimeout(resolve, delay));
					continue;
				}
				
				// If it's not a 500 error, or we're out of retries, throw the error
				throw error;
			}
		}
		throw new Error('Maximum retries exceeded');
	};

	const getOne = async <T>(relativeUri: string): Promise<T> => {
		return retryRequest(async () => {
			const token = await getAccessTokenSilently({
				authorizationParams: {
					audience: 'https://prestige-auth0-resource',
				},
				cacheMode: "on"
			})
			console.log("API URL:", apiUri);
			console.log("Endpoint:", relativeUri);
			console.log("Token (first 50 chars):", token?.substring(0, 50) + "...");
			
			const response = await axios.get<T>(`${apiUri}/${relativeUri}`, {
				headers: {
					Authorization: "Bearer " + token,
				},
			})
			console.log("Response status:", response.status);
			return response.data as T
		});
	}

	const getMany = async <T>(relativeUri: string): Promise<T[]> => {
		return retryRequest(async () => {
			const token = await getAccessTokenSilently({
				authorizationParams: {
					audience: 'https://prestige-auth0-resource',
				},
				cacheMode: "on"
			})

			const response = await axios.get<T[]>(`${apiUri}/${relativeUri}`, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data
		});
	}

	const post = async <T, S>(rq: S, relativeUri: string): Promise<T> => {
		return retryRequest(async () => {
			const token = await getAccessTokenSilently({
				authorizationParams: {
					audience: 'https://prestige-auth0-resource',
				},
				cacheMode: "on"
			})
			const response = await axios.post<T>(`${apiUri}/${relativeUri}`, rq, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data as T
		});
	}

	const patch = async <T, S>(rq: S, relativeUri: string): Promise<T> => {
		return retryRequest(async () => {
			const token = await getAccessTokenSilently({
				authorizationParams: {
					audience: 'https://prestige-auth0-resource',
				},
				cacheMode: "on"
			})
			const response = await axios.patch<T>(`${apiUri}/${relativeUri}`, rq, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data
		});
	}

	const put = async <T, S>(rq: S, relativeUri: string): Promise<T> => {
		return retryRequest(async () => {
			const token = await getAccessTokenSilently({
				authorizationParams: {
					audience: 'https://prestige-auth0-resource',
				},
				cacheMode: "on"
			})
			const response = await axios.put<T>(`${apiUri}/${relativeUri}`, rq, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data
		});
	}

	const deleteOne = async <T>(relativeUri: string): Promise<T> => {
		return retryRequest(async () => {
			const token = await getAccessTokenSilently({
				authorizationParams: {
					audience: 'https://prestige-auth0-resource',
				},
				cacheMode: "on"
			})
			const response = await axios.delete<T>(`${apiUri}/${relativeUri}`, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data
		});
	}
	
	// Back-compat: expose both deleteOne and del
	const del = deleteOne;
	return { getOne, getMany, post, patch, put, deleteOne, del }
}

export default useHttp
