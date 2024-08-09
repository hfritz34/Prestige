import useProfile from "@/hooks/useProfile";
import { useQuery } from "@tanstack/react-query";

const RecentlyPlayed : React.FC = () => {
    const profile = useProfile();

    const RecentlyPlayedQuery = () => {
        return useQuery({
            queryKey: ["recent"],
            queryFn: async () => {
                const response = await profile.getRecentlyPlayed();
                return response;
            }
        });
    }
    const recentlyPlayed = RecentlyPlayedQuery().data;

    return (
        <>
        <h2 className="text-lg font-bold mt-6 mb-5">Recently Played</h2><ul>
            {recentlyPlayed?.map((track) => (
                <li key={track.id} className="flex items-center ml-2.5 mb-2.5">
                    <img src={track.imageUrl} alt={track.trackName} className="w-16 h-16 mr-2.5" />
                    <div>
                        <h3 className="m-0 text-lg">{track.trackName}</h3>
                        <p className="m-0 text-gray-600 mb-5">{track.artistName}</p>
                    </div>
                </li>
            ))}
        </ul>
        </>  
    )
}

export default RecentlyPlayed;