import { useState } from "react";
import { CarouselItem } from "@/components/ui/carousel";
import { UserTrackResponse } from "@/hooks/useProfile";
import FriendItemModal from "@/components/ui/friend-item-modal";

type FriendTrackCarouselItemProps = {
    userTrack: UserTrackResponse;
    friendNickname: string;
};

const FriendTrackCarouselItem: React.FC<FriendTrackCarouselItemProps> = ({ userTrack, friendNickname }) => {
    const [showModal, setShowModal] = useState(false);

    const handleTrackClick = () => {
        setShowModal(true);
    };

    return (
        <>
            <CarouselItem key={userTrack.track.id} className="basis-1/3">
                <div className="track-item cursor-pointer hover:opacity-80 transition-opacity" onClick={handleTrackClick}>
                    <img 
                        src={userTrack.track.album.images[0]?.url || "/placeholder-album.png"} 
                        alt={userTrack.track.name}
                        className="w-full aspect-square object-cover rounded-lg mb-2"
                    />
                    <h3 className="text-sm font-bold line-clamp-2">{userTrack.track.name}</h3>
                    <div className="text-xs text-gray-400 line-clamp-1">
                        {userTrack.track.artists.map(artist => artist.name).join(', ')}
                    </div>
                    <p className="text-xs text-gray-500 mt-1">{Math.floor(userTrack.totalTime / 60)} min</p>
                </div>
            </CarouselItem>
            
            <FriendItemModal
                isOpen={showModal}
                onClose={() => setShowModal(false)}
                item={userTrack}
                itemType="track"
                friendNickname={friendNickname}
            />
        </>
    );
};

export default FriendTrackCarouselItem;