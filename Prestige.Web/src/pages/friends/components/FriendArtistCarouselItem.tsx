import { useState } from "react";
import { CarouselItem } from "@/components/ui/carousel";
import { UserArtistResponse } from "@/hooks/useProfile";
import FriendItemModal from "@/components/ui/friend-item-modal";

type FriendArtistCarouselItemProps = {
    userArtist: UserArtistResponse;
    friendNickname: string;
};

const FriendArtistCarouselItem: React.FC<FriendArtistCarouselItemProps> = ({ userArtist, friendNickname }) => {
    const [showModal, setShowModal] = useState(false);

    const handleArtistClick = () => {
        setShowModal(true);
    };

    return (
        <>
            <CarouselItem key={userArtist.artist.id} className="basis-1/3">
                <div className="artist-item cursor-pointer hover:opacity-80 transition-opacity" onClick={handleArtistClick}>
                    <img 
                        src={userArtist.artist.images[0]?.url || "/placeholder-album.png"} 
                        alt={userArtist.artist.name}
                        className="w-full aspect-square object-cover rounded-full mb-2"
                    />
                    <h3 className="text-sm font-bold line-clamp-2 text-center">{userArtist.artist.name}</h3>
                    <p className="text-xs text-gray-500 mt-1 text-center">{Math.floor(userArtist.totalTime / 60)} min</p>
                </div>
            </CarouselItem>
            
            <FriendItemModal
                isOpen={showModal}
                onClose={() => setShowModal(false)}
                item={userArtist}
                itemType="artist"
                friendNickname={friendNickname}
            />
        </>
    );
};

export default FriendArtistCarouselItem;