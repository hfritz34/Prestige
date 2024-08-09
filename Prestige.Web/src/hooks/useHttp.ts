/* eslint-disable no-unused-expressions */
import axios, { AxiosError } from 'axios'
import { useAuth0 } from '@auth0/auth0-react'

const useHttp = () => {
	const { getAccessTokenSilently } = useAuth0();
    const apiUri = import.meta.env.VITE_API_ADDRESS;

	const getOne = async <T>(relativeUri: string): Promise<T> => {
		const token = await getAccessTokenSilently({
            authorizationParams: {
                audience: 'https://prestigeapi.azurewebsites.net',
              },
            cacheMode: "on"
        })
		const response = await axios.get<T>(`${apiUri}/${relativeUri}`, {
			headers: {
				Authorization: "Bearer " + token,
			},
		})
		return response.data as T
	}

	const getMany = async <T>(relativeUri: string): Promise<T[]> => {
		const token = await getAccessTokenSilently({
            authorizationParams: {
                audience: 'https://prestigeapi.azurewebsites.net',
              },
            cacheMode: "on"
        })

		const response = await axios.get<T[]>(`${apiUri}/${relativeUri}`, {
			headers: {
				Authorization: `Bearer ${token}`,
			},
		})
		return response.data
	}

	const post = async <T, S>(rq: S, relativeUri: string): Promise<T> => {
		try {
			const token = await getAccessTokenSilently({
                authorizationParams: {
                    audience: 'https://prestigeapi.azurewebsites.net',
                  },
                cacheMode: "on"
            })
			const response = await axios.post<T>(`${apiUri}/${relativeUri}`, rq, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data as T
		} catch (error: unknown) {
			const err = (error as AxiosError)
			throw err
		}
	}

	const patch = async <T, S>(rq: S, relativeUri: string): Promise<T> => {
		try {
			const token = await getAccessTokenSilently({
                authorizationParams: {
                    audience: 'https://prestigeapi.azurewebsites.net',
                  },
                cacheMode: "on"
            })
			const response = await axios.patch<T>(`${apiUri}/${relativeUri}`, rq, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data
		} catch (error: unknown) {
			const err = error as AxiosError
            throw err
		}
		return {} as T
	}

	const put = async <T, S>(rq: S, relativeUri: string): Promise<T> => {
		try {
			const token = await getAccessTokenSilently({
                authorizationParams: {
                    audience: 'https://prestigeapi.azurewebsites.net',
                  },
                cacheMode: "on"
            })
			const response = await axios.put<T>(`${apiUri}/${relativeUri}`, rq, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data
		} catch (error: unknown) {
			const err = error as AxiosError
            throw err
		}
		return {} as T
	}

	const deleteOne = async <T>(relativeUri: string): Promise<T> => {
		try {
			const token = await getAccessTokenSilently({
                authorizationParams: {
                    audience: 'https://prestigeapi.azurewebsites.net',
                  },
                cacheMode: "on"
            })
			const response = await axios.delete<T>(`${apiUri}/${relativeUri}`, {
				headers: {
					Authorization: `Bearer ${token}`,
				},
			})
			return response.data
		} catch (error: unknown) {
			const err = error as AxiosError
            throw err
		}
		return {} as T
	}
	return { getOne, getMany, post, patch, put, deleteOne }
}

export default useHttp
