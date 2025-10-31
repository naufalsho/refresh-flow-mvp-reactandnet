import axios from 'axios'
import type {
    AxiosError,
    AxiosInstance,
    AxiosRequestConfig,
    AxiosResponse,
    InternalAxiosRequestConfig,
} from 'axios'

//const baseURL =
//    import.meta.env.DEV ? '' : ((import.meta.env.VITE_API_BASE as string | undefined) ?? '')
const baseURL = import.meta.env.VITE_API_BASE as string | undefined
export const api: AxiosInstance = axios.create({
    baseURL,
    withCredentials: true,
})

let accessToken: string | undefined
let isRefreshing = false
let refreshPromise: Promise<void> | null = null
const requestQueue: Array<(token?: string) => void> = []

export function setAccessToken(token?: string) {
    accessToken = token
}

function onRefreshed(newToken?: string) {
    requestQueue.forEach((cb) => cb(newToken))
    requestQueue.length = 0
}
function enqueue(cb: (token?: string) => void) {
    requestQueue.push(cb)
}

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    if (accessToken) {
        config.headers = config.headers ?? {}
            ; (config.headers as any).Authorization = `Bearer ${accessToken}`
    }
    return config
})

async function performRefresh(): Promise<void> {
    const res = await axios.post('/auth/refresh', null, {
        baseURL: api.defaults.baseURL || '',
        withCredentials: true,
    })
    const newToken = (res.data?.accessToken as string | undefined) ?? undefined
    if (newToken) setAccessToken(newToken)
}

api.interceptors.response.use(
    (r: AxiosResponse) => r,
    async (error: AxiosError) => {
        const original = error.config as (AxiosRequestConfig & { _retry?: boolean }) | undefined
        if (error.response?.status === 401 && original && !original._retry) {
            original._retry = true

            if (!isRefreshing) {
                isRefreshing = true
                refreshPromise = performRefresh()
                    .catch(() => { })
                    .finally(() => {
                        isRefreshing = false
                    })
            }

            try {
                await refreshPromise
                return await new Promise<AxiosResponse>((resolve, reject) => {
                    enqueue(async () => {
                        if (!accessToken) return reject(error)
                        try {
                            resolve(await api(original))
                        } catch (e) {
                            reject(e)
                        }
                    })
                    onRefreshed(accessToken)
                })
            } catch {
                return Promise.reject(error)
            }
        }
        return Promise.reject(error)
    }
)
